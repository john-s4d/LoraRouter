using System.Net.Sockets;
using System.Net;
using System.Text.Json;

using static LoraRouter.SemtechUdp.SemtechPacket;
using static LoraRouter.SemtechUdp.SemtechRxPacket;

namespace LoraRouter.SemtechUdp
{
    public class SemtechUdpConfig : ConfigBase
    {
        public const string PORT = "port";
        public SemtechUdpConfig()
        {
            this[PORT] = 1700;
        }
    }

    public class SemtechUdpBridge : IBridge, IConfigurable<SemtechUdpConfig>
    {
        public event PacketReceivedEventHandler? PacketReceivedEvent;

        public bool IsStarted { get; private set; }
        public SemtechUdpConfig Config { get => _config; set => _config = value; }
        public string BridgeId { get; set; } = "SemtechUdpBridge";

        private SemtechUdpConfig _config;
        private Thread? _listenThread;
        UdpClient? _listenClient;
        private ILogger? _logger;

        private Dictionary<string, IPEndPoint> _gatewayEndpoints = new Dictionary<string, IPEndPoint>();
        //private Dictionary<string, Gateway> _gateways = new Dictionary<string, Gateway>();

        public SemtechUdpBridge(SemtechUdpConfig config, ILogger? logger)
        {
            _config = config;
            _logger = logger;
        }

        public SemtechUdpBridge(ILogger? logger)
            : this(new SemtechUdpConfig(), logger)
        { }

        public void Start()
        {
            if (IsStarted) { return; }

            _logger?.Write(this, LogLevel.INFO, "starting SemtechUdp listener");

            IsStarted = true;

            _listenThread = new Thread(new ThreadStart(Receive));

            _listenThread.Start();
        }

        private void Receive()
        {
            _listenClient = new UdpClient((int)_config[SemtechUdpConfig.PORT]);

            _logger?.Write(this, LogLevel.INFO, "SemtechUdp listener thread started");

            while (IsStarted)
            {
                try
                {
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, (int)_config[SemtechUdpConfig.PORT]);
                    byte[] data = _listenClient.Receive(ref endPoint);
                    DataReceived(data, endPoint);
                }
                catch (SocketException ex)
                {
                    _logger?.Write(this, LogLevel.ERROR, ex.ErrorCode == 10060 ? "timeout error" : $"serious error: {ex.ErrorCode}");
                }
            }
        }

        private void DataReceived(byte[] data, IPEndPoint endPoint)
        {
            SemtechPacket packet = new SemtechPacket().FromByteArray(data);

            if ((packet.Type == PacketType.PUSH_DATA && packet.Json.StartsWith("{\"rxpk\":")) || packet.Type == PacketType.TX_ACK)
            {
                _logger?.Write(this, LogLevel.DEBUG, $"IN  | {packet.Type} | {endPoint.Address}:{endPoint.Port} | {packet.Token} {packet.GatewayId} {packet.Json}");
            }

            _logger?.Write(this, LogLevel.FINEST, $"Packet Received: {packet.Type} from {packet.GatewayId} with token {packet.Token}");

            if (!string.IsNullOrEmpty(packet.Json))
            {
                _logger?.Write(this, LogLevel.FINEST, $"JSON Received: {packet.Json}");
            }

            if (packet.Type == PacketType.PULL_DATA)
            {
                UpdateEndPoint(packet.GatewayId, endPoint);
                Send(new SemtechPacket(PacketType.PULL_ACK, packet.Token), endPoint);
            }

            if (packet.Type == PacketType.PUSH_DATA)
            {
                Send(new SemtechPacket(PacketType.PUSH_ACK, packet.Token), endPoint);

                RxPacket rfPacket = JsonSerializer.Deserialize<RxPacket>(packet.Json)!;

                if (rfPacket.rxpk != null)
                {
                    foreach (RxPacket.RxPk rx in rfPacket.rxpk)
                    {
                        PacketReceivedEvent?.Invoke(this, new PacketReceivedEventArgs(rx));
                    }
                }
            }

            if (packet.Type == PacketType.TX_ACK)
            {
                // Just ignore this                
            }
        }

        private void UpdateEndPoint(string gatewayId, IPEndPoint endPoint)
        {
            if (!_gatewayEndpoints.ContainsKey(gatewayId))
            {
                _gatewayEndpoints.Add(gatewayId, endPoint);
            }

            if (!endPoint.Equals(_gatewayEndpoints[gatewayId]))
            {
                _gatewayEndpoints[gatewayId] = endPoint;
                _logger?.Write(this, LogLevel.INFO, $"Gateway with ID {gatewayId} found at {endPoint.Address}");
            }
        }

        public void Send(IPacket packet)
        {

            throw new NotImplementedException();
        }

        public void Send(SemtechPacket packet, IPEndPoint endPoint)
        {
            if (packet.Type == SemtechPacket.PacketType.PULL_RESP)
            {
                _logger?.Write(this, LogLevel.DEBUG, $"OUT | {packet.Type} | {endPoint.Address}:{endPoint.Port} | {packet.Token} {packet.GatewayId} {packet.Json}");
            }

            Send(packet.ToByteArray(), endPoint);
        }

        public async void Send(byte[] data, IPEndPoint endPoint)
        {
            if (!IsStarted || _listenClient == null || _listenThread == null)
            {
                _logger?.Write(this, LogLevel.ERROR, $"SemtechUdp Send error: Not Connected.");
                return;
            }
            try
            {

                await _listenClient.SendAsync(data, data.Length, endPoint);
            }
            catch (Exception e)
            {
                _logger?.Write(this, LogLevel.ERROR, $"SemtechUdp Send error: {e}");
            }
        }

        public void Stop()
        {
            if (!IsStarted || _listenClient == null || _listenThread == null) { return; }

            _logger?.Write(this, LogLevel.INFO, "stopping SemtechUdp listener");

            IsStarted = false;
            _listenClient.Close();
            _listenThread.Join(5000);
            _listenThread = null;

            _logger?.Write(this, LogLevel.INFO, "SemtechUdp listener thread stopped");
        }

        public T CloneAs<T>(IPacket packet)
        {
            throw new NotImplementedException();
        }
    }
}
