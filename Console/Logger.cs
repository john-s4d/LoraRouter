using System.Text;

namespace LoraRouter
{
    public class LoggerConfig : ConfigBase
    {
        public const string LOG_FILE = "logFile";
    }

    public class Logger : ILogger, IConfigurable<LoggerConfig>
    {
        public event LogEventHandler? EventLogged;

        private const LogLevel DEFAULT = LogLevel.INFO;
        private const long MAX_LENGTH = 0x100000L;

        private static Mutex _mutex = new Mutex(false);

        private LoggerConfig _config;

        public LogLevel FileLevel { get; set; } = LogLevel.DEBUG;
        public LoggerConfig Config { get => _config; set => _config = value; }


        public Logger() : this(new LoggerConfig()) { }

        public Logger(LoggerConfig config)
        {
            _config = config;
        }

        private async Task WriteToFile(LogLevel level, string line)
        {
            if (level > FileLevel) { return; }

            bool rollFile;

            _mutex.WaitOne();

            using (var file = new StreamWriter((string)_config[LoggerConfig.LOG_FILE], append: true))
            {
                await file.WriteLineAsync(line);

                rollFile = file.BaseStream.Length > MAX_LENGTH;
            }

            if (rollFile)
            {
                FileInfo fi = new FileInfo((string)_config[LoggerConfig.LOG_FILE]);
                if (fi.Exists)
                {
                    fi.MoveTo($"{(string)_config[LoggerConfig.LOG_FILE]}_{DateTime.Now.ToFileTimeUtc()}.txt");
                }
            }
            _mutex.ReleaseMutex();

        }
        private void OnLogEvent(LogLevel level, string message)
        {
            EventLogged?.Invoke(this, new LogEventArgs(level, message));
        }

        public void Write(object sender, LogLevel level, string message)
        {
            message = $"{DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss.fff")} | {message}";
            WriteToFile(level, message).Wait();
            OnLogEvent(level, message);
        }

        public void Write(object sender, LogLevel level, byte[] bytes, Encoding encoding)
        {
            Write(sender, level, encoding.GetString(bytes));
        }

        public void Write(object sender, LogLevel level, Exception ex)
        {
            Write(sender, level, ex.Message);
            Write(sender, LogLevel.DEBUG, ex.StackTrace);
        }

        public void Write(object sender, string line)
        {
            Write(sender, DEFAULT, line);
        }

        public void Write(object sender, byte[] bytes, Encoding encoding)
        {
            Write(sender, DEFAULT, bytes, encoding);
        }

        public void Write(object sender, Exception ex)
        {
            Write(sender, DEFAULT, ex);
        }
    }
}
