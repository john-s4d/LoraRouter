namespace LoraRouter
{
    public interface IPacketUp : IPacket
    {
        string? Payload { get; set; }
        string? DeviceId { get; set; }
        float? Frequency { get; set; }
        int? Rssi { get; set; }
    }
}
