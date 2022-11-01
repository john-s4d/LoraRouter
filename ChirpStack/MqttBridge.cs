using System.Security.Cryptography.X509Certificates;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace LoraRouter.ChirpStack
{
    public class MqttConfig : ConfigBase
    {
        public const string HOST = "host";
        public const string PORT = "port";
        public const string CLIENT_ID = "clientId";
        public const string CA_CERT_FILE = "caCertFile";
        public const string CLIENT_CERT_FILE = "clientCertFile";

        public MqttConfig() { }

    }    

    public class MqttBridge : IBridge, IConfigurable<MqttConfig>
    {
        public virtual event PacketReceivedEventHandler? PacketReceivedEvent;

        private ILogger? _logger;
        private MqttClient? _mqttClient;
        private MqttConfig _config;
        public MqttConfig Config { get => _config; set => _config = value; }
        public string BridgeId { get; set; } = "MqttBridge";

        public bool IsStarted { get; private set; }

        public MqttBridge(MqttConfig config, ILogger? logger)
        {
            _logger = logger;
            _config = config;
        }

        public virtual void Start()
        {
            if (IsStarted) { return; }

            _logger?.Write(this, LogLevel.INFO, "Starting MqttBridge");

            IsStarted = true;

            _mqttClient = new MqttClient(
                (string)_config[MqttConfig.HOST],
                (int)_config[MqttConfig.PORT],
                true,
                X509Certificate.CreateFromCertFile((string)_config[MqttConfig.CA_CERT_FILE]),
                X509Certificate.CreateFromCertFile((string)_config[MqttConfig.CLIENT_CERT_FILE]),
                MqttSslProtocols.TLSv1_2
            );

            _mqttClient.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            _mqttClient.MqttMsgSubscribed += Client_MqttMsgSubscribed;

            _mqttClient.Connect((string)_config[MqttConfig.CLIENT_ID]);
        }

        public void Subscribe(string[] topics)
        {
            List<byte> qosLevels = new List<byte>();
            
            for (int i = 0; i < topics.Length; i++)
            {
                qosLevels.Add(MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE);
            }

            _mqttClient?.Subscribe(topics, qosLevels.ToArray());

            _logger?.Write(this, $"MqttBridge.Subscribed Started: {string.Join(", ", topics)}");

        }

        public void Unsubscribe(string[] topics)
        {
            _ = _mqttClient ?? throw new MqttConnectionException("MqttBridge.Unsubscribe", null);

            _mqttClient?.Unsubscribe(topics);

            _logger?.Write(this, $"MqttBridge.Unsubscribe Started: {string.Join(", ", topics)}");
        }

        public virtual void Stop()
        {
            if (!IsStarted) { return; }

            _logger?.Write(this, LogLevel.INFO, "Stopping MqttBridge");

            IsStarted = false;

            _mqttClient.Disconnect();

            _mqttClient.MqttMsgPublishReceived -= Client_MqttMsgPublishReceived;
            _mqttClient.MqttMsgSubscribed -= Client_MqttMsgSubscribed;
            _mqttClient.MqttMsgUnsubscribed -= Client_MqttMsgUnsubscribed;
        }

        public virtual void Send(IPacket packet)
        {
            if (packet.GetType() == typeof(MqttPacket))
            {
                Send((MqttPacket)packet);
            }
            else
            {
                throw new InvalidCastException(nameof(packet));
            }
        }

        protected void Send(MqttPacket packet)
        {
            _mqttClient.Publish(packet.Topic, Encoding.UTF8.GetBytes(packet.Json));

            _logger?.Write(this, $"MqttBridge: Published to {packet.Topic} with JSON {packet.Json}");
        }

        private void Client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            _logger?.Write(this, $"MqttBridge.Subscribe Complete");
        }

        private void Client_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {            
            _logger?.Write(this, $"MqttBridge.Unsubscribe Complete");
        }

        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            PacketReceivedEvent?.Invoke(sender, new PacketReceivedEventArgs(new MqttPacket(e.Topic, Encoding.UTF8.GetString(e.Message))));
        }

        public virtual T CloneAs<T>(IPacket packet)
        {
            throw new NotImplementedException();
        }
    }
}