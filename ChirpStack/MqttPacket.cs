namespace LoraRouter.ChirpStack
{
    public class MqttPacket : IPacket
    {
        public string Topic { get; set; }
        public string Json { get; set; }

        public MqttPacket(string topic, string json)
        {
            Topic = topic;
            Json = json;
        }
    }
}
