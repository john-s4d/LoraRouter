namespace LoraRouter
{
    public class ConfigBase
    {
        private Dictionary<string, IConvertible> _config = new Dictionary<string, IConvertible>();

        public IConvertible this[string key]
        {
            get => _config.ContainsKey(key) ? _config[key] : string.Empty;

            set => _config[key] = value; 
        }
    }
}
