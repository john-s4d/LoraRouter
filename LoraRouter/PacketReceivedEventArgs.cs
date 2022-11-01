namespace LoraRouter
{
    public delegate void PacketReceivedEventHandler(object sender, PacketReceivedEventArgs e);

    public class PacketReceivedEventArgs : EventArgs         
    {   
        public IPacket Packet { get; set; } 
        public PacketReceivedEventArgs(IPacket packet)
        {
            Packet = packet;
        }
    }
}
