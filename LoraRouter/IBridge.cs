namespace LoraRouter
{
    public interface IBridge
    {
        event PacketReceivedEventHandler PacketReceivedEvent;
        string BridgeId { get; set; }
        bool IsStarted { get; }
        T CloneAs<T>(IPacket packet);
        void Send(IPacket packet);
        void Start();
        void Stop();
        
    }
}