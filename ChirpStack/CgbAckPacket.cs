namespace LoraRouter.ChirpStack
{
    internal class CgbAckPacket : IPacketAck
    {
        internal string? gatewayID { get; set; }
        internal int? token { get; set; }
        internal IEnumerable<item> items { get; set; } = new List<item>();

        internal class item
        {
            public string? status { get; set; }
        }
    }
}
