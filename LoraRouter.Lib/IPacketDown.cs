namespace LoraRouter
{
    public interface IPacketDown : IPacket
    {
        string? DeviceId { get; set; }
        uint? Token { get; set; }
        int? Power { get; set; }
    }
}
