namespace LoraRouter
{
    public class LoraRouter
    {
        private const int DEFAULT_RSSI = -90;

        private ILogger? _logger;
        private Deduplicator _deduplicator = new Deduplicator();

        private Dictionary<string, List<string>> _rules = new Dictionary<string, List<string>>();
        private Dictionary<string, IBridge> _routes = new Dictionary<string, IBridge>();
        private Dictionary<string, Gateway> _gateways = new Dictionary<string, Gateway>();

        public LoraRouter(ILogger? logger)
        {
            _logger = logger;
        }

        private void _bridge_PacketReceivedEvent(object sender, PacketReceivedEventArgs e)
        {
            if (e == null || e.Packet == null) { return; }

            //_logger?.Write(this, LogLevel.FINEST, $"Packet Received: {e.Packet.GetType()} from {e.GatewayId} with JSON: {e.Packet.Json}");

            if ((typeof(IPacketUp).IsAssignableFrom(e.Packet.GetType())))
            {
                IPacketUp up = (IPacketUp)e.Packet;

                if (up.Payload == null || up.DeviceId == null) { return; }

                if (_deduplicator.IsDuplicate(up.Payload, up.DeviceId))
                {
                    _logger?.Write(this, LogLevel.DEBUG, $"Skipped Duplicate Packet from {up.DeviceId}");
                    return;
                }

                if (_rules.ContainsKey(up.DeviceId))
                {
                    foreach (string destinationId in _rules[up.DeviceId])
                    {
                        if (!_routes.ContainsKey(destinationId))
                        {
                            _logger?.Write(this, LogLevel.WARNING, $"Skipped Packet. No route to available to {destinationId}");
                            continue;
                        }

                        if (_deduplicator.IsDuplicate(up.Payload, destinationId))
                        {
                            _logger?.Write(this, LogLevel.DEBUG, $"Skipped Duplicate Packet to {destinationId}");
                            continue;
                        }

                        if (!_gateways[destinationId].CanTxRadio0((up.Frequency ?? 0 / 1000)))
                        {
                            _logger?.Write(this, LogLevel.DEBUG, $"No TxChannel for {up.Frequency} on {destinationId}");
                            continue;
                        }

                        // TODO: Convert to the type specific to destination bridge                                        

                        IPacketDown down = _routes[destinationId].CloneAs<IPacketDown>(up);

                        down.DeviceId = destinationId;
                        down.Token = Utils.RandomUint();
                        down.Power = Convert.ToInt32(_gateways[destinationId].GetPower(up.Rssi ?? DEFAULT_RSSI));

                        _routes[destinationId].Send(down);

                    }                    
                }
            }

            // TODO: Implement Stats && Ack

            /*
            if (e.Packet.GetType() == typeof(CgbStatsPacket))
            {
                _logger?.Write(this, $"Stats: {((CgbStatsPacket)e.Packet).gatewayID}");
            }

            if (e.Packet.GetType() == typeof(CgbAckPacket))
            {
                _logger?.Write(this, LogLevel.FINEST, $"Token: {((CgbAckPacket)e.Packet).token}");
            }*/
        }

        public void AddRoute(string sourceGatewayId, string destinationGatewayId)
        {
            if (!_rules.ContainsKey(sourceGatewayId))
            {
                _rules.Add(sourceGatewayId, new List<string>());
            }

            if (!_rules[sourceGatewayId].Contains(destinationGatewayId))
            {
                _rules[sourceGatewayId].Add(destinationGatewayId);
            }
        }

        public void AddBridgeWithGateways(IBridge bridge, Gateway[] gateways)
        {
            bridge.PacketReceivedEvent += _bridge_PacketReceivedEvent;

            foreach (Gateway gateway in gateways)
            {
                _gateways.Add(gateway.Id, gateway);
                _routes.Add(gateway.Id, bridge);
            }
        }

        public void Start()
        {
            foreach (IBridge bridge in _routes.Values)
            {
                bridge.Start();
            }
        }

        public void Stop()
        {
            foreach (IBridge bridge in _routes.Values)
            {
                bridge.Stop();
            }
        }
    }
}
