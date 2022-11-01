using LoraRouter;
using System.Text.Json;

namespace LoraRouter.ChirpStack
{
    public class ChirpstackConfig : ConfigBase
    {
        public MqttConfig MqttConfig { get; set; }

        public ChirpstackConfig() : this(new MqttConfig()) { }
        public ChirpstackConfig(MqttConfig mqttConfig)
        {
            MqttConfig = mqttConfig;
        }
    }

    public class ChirpstackBridge : MqttBridge, IConfigurable<ChirpstackConfig>
    {

        private const string UP_TOPIC = "gateway/+/event/up";
        private const string STATS_TOPIC = "gateway/+/event/stats";
        private const string ACK_TOPIC = "gateway/+/event/ack";
        private const string DOWN_TOPIC = "gateway/+/command/down";
        private const string SINGLE_WILD = "+";
        private const string MULTI_WILD = "#";

        public override event PacketReceivedEventHandler? PacketReceivedEvent;

        private ILogger? _logger;
        private ChirpstackConfig _config;

        public new ChirpstackConfig Config { get => _config; set => _config = value; }
        public new string BridgeId { get; set; } = "ChirpstackBridge";
        public new bool IsStarted { get; set; }

        public ChirpstackBridge(ILogger logger)
            : this(new ChirpstackConfig(), logger)
        { }

        public ChirpstackBridge(ChirpstackConfig config, ILogger? logger)
            : base(config.MqttConfig, logger)
        {
            _config = config;
            _logger = logger;

            base.PacketReceivedEvent += ChirpstackBridge_PacketReceivedEvent;
        }


        private void ChirpstackBridge_PacketReceivedEvent(object sender, PacketReceivedEventArgs e)
        {
            MqttPacket mqttPacket = (MqttPacket)e.Packet;

            IPacket? cbgPacket = null;


            if (MqttTopicFilterComparer.Compare(mqttPacket.Topic, STATS_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
            {
                CgbStatsPacket stats = JsonSerializer.Deserialize<CgbStatsPacket>(mqttPacket.Json)!;
                stats.gatewayID = Utils.ToHexString(Convert.FromBase64String(stats.gatewayID!));
                cbgPacket = stats;
            }

            else if (MqttTopicFilterComparer.Compare(mqttPacket.Topic, UP_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
            {
                CgbUpPacket up = JsonSerializer.Deserialize<CgbUpPacket>(mqttPacket.Json)!;
                up.rxInfo.gatewayID = Utils.ToHexString(Convert.FromBase64String(up.rxInfo.gatewayID!));
                up.txInfo.frequency = up.txInfo.frequency / 1000000;
                cbgPacket = up;
            }

            else if (MqttTopicFilterComparer.Compare(mqttPacket.Topic, ACK_TOPIC) == MqttTopicFilterCompareResult.IsMatch)
            {
                CgbAckPacket ack = JsonSerializer.Deserialize<CgbAckPacket>(mqttPacket.Json)!;
                ack.gatewayID = Utils.ToHexString(Convert.FromBase64String(ack.gatewayID!));
                cbgPacket = ack;
            }

            PacketReceivedEvent?.Invoke(sender, new PacketReceivedEventArgs(cbgPacket ?? throw new NullReferenceException(nameof(cbgPacket))));
        }

        public override void Start()
        {
            if (IsStarted) { return; }

            _logger?.Write(this, LogLevel.INFO, "Starting ChirpstackBridge");

            IsStarted = true;

            base.Start();

            Subscribe(new string[] { UP_TOPIC, ACK_TOPIC, STATS_TOPIC });
        }

        public override void Stop()
        {
            if (!IsStarted) { return; }

            _logger?.Write(this, LogLevel.INFO, "Stopping ChirpstackBridge");

            IsStarted = false;

            Unsubscribe(new string[] { UP_TOPIC, ACK_TOPIC, STATS_TOPIC });

            base.Stop();
        }

        private void Send(CgbDownPacket packet)
        {
            string topic = DOWN_TOPIC.Replace(SINGLE_WILD, packet.gatewayID);

            packet.gatewayID = Convert.ToBase64String(Utils.FromHexString(packet.gatewayID!));

            Send(new MqttPacket(topic, JsonSerializer.Serialize(packet)));

            _logger?.Write(this, $"ChirpstackBridge: Published");
        }

        public override void Send(IPacket packet)
        {
            if (packet.GetType() == typeof(CgbDownPacket))
            {
                Send((CgbDownPacket)packet);
            }
            else
            {
                base.Send(packet);
            }
        }

        public override T CloneAs<T>(IPacket packet)
        {
            if (packet.GetType() == typeof(CgbUpPacket) && typeof(T).IsAssignableFrom(typeof(IPacketDown)))
            {
                CgbUpPacket up = (CgbUpPacket)packet;

                CgbDownPacket.Item item = new CgbDownPacket.Item();

                item.txInfo.timing = "IMMEDIATELY";
                item.txInfo.frequency = up.txInfo.frequency;
                item.txInfo.antenna = 0;
                item.txInfo.board = 0;
                item.txInfo.modulation = up.txInfo.modulation;
                item.txInfo.loRaModulationInfo.spreadingFactor = up.txInfo.loRaModulationInfo.spreadingFactor;
                item.txInfo.loRaModulationInfo.polarizationInversion = up.txInfo.loRaModulationInfo.polarizationInversion;
                item.txInfo.loRaModulationInfo.bandwidth = up.txInfo.loRaModulationInfo.bandwidth;
                item.txInfo.loRaModulationInfo.codeRate = up.txInfo.loRaModulationInfo.codeRate;
                item.phyPayload = up.phyPayload;

                CgbDownPacket down = new CgbDownPacket();
                down.items.Add(item);

                return (T)(IPacketDown)down;
            }

            throw new NotImplementedException();
        }
    }
}