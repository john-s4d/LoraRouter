using System.Text.Json.Serialization;

namespace LoraRouter.ChirpStack
{
    public class CgbUpPacket : IPacketUp
    {
        #region IPacketUp

        [JsonIgnore]
        public string? Payload { get => phyPayload; set => phyPayload = value; }
        [JsonIgnore]
        public string? DeviceId { get => rxInfo.gatewayID; set => rxInfo.gatewayID = value; }
        [JsonIgnore]
        public float? Frequency { get => txInfo.frequency; set => txInfo.frequency = value; }
        [JsonIgnore]
        public int? Rssi { get => rxInfo.rssi; set => rxInfo.rssi = value; }

        #endregion

        public string? phyPayload { get; set; }
        public TxInfo txInfo { get; set; } = new TxInfo();
        public RxInfo rxInfo { get; set; } = new RxInfo();

        public class TxInfo
        {
            public float? frequency { get; set; }
            public string? modulation { get; set; }
            public LoRaModulationInfo loRaModulationInfo { get; set; } = new LoRaModulationInfo();

            public class LoRaModulationInfo
            {
                public int? bandwidth { get; set; }
                public int? spreadingFactor { get; set; }
                public string? codeRate { get; set; }
                public bool? polarizationInversion { get; set; }
            }
        }

        public class RxInfo
        {
            public string? gatewayID { get; set; }
            public DateTime? time { get; set; }
            public int? timestamp { get; set; }
            public int? rssi { get; set; }
            public float? loRaSNR { get; set; }
            public int? channel { get; set; }
            public int? rfChain { get; set; }
            public int? board { get; set; }
            public int? antenna { get; set; }
            public string? fineTimestampType { get; set; }
            public EncryptedFineTimestamp encryptedFineTimestamp { get; set; } = new EncryptedFineTimestamp();

            public class EncryptedFineTimestamp
            {
                public int? aesKeyIndex { get; set; }
                public string? encryptedNS { get; set; }
            }
        }
    }
}
