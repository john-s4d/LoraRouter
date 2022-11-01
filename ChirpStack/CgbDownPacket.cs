using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;

namespace LoraRouter.ChirpStack
{
    internal class CgbDownPacket : IPacketDown
    {

        #region IPacketDown

        [JsonIgnore]
        public int? Power { get => items.FirstOrDefault()!.txInfo.power; set => items.FirstOrDefault()!.txInfo.power = value; }
        [JsonIgnore]
        public string? DeviceId { get => gatewayID; set => gatewayID = value; }
        [JsonIgnore]
        public uint? Token { get => token; set => token = value; }

        #endregion


        public string? gatewayID { get; set; }
        public uint? token { get; set; }
        public List<Item> items { get; set; } = new List<Item>();

        public class Item
        {
            public string? phyPayload { get; set; }
            public TxInfo txInfo { get; set; } = new TxInfo();

            public class TxInfo
            {
                public float? frequency { get; set; }
                public int? power { get; set; }
                public string? modulation { get; set; }
                public LoRaModulationInfo loRaModulationInfo { get; set; } = new LoRaModulationInfo();
                public int? board { get; set; }
                public int? antenna { get; set; }
                public string? timing { get; set; }

                public class LoRaModulationInfo
                {
                    public int? bandwidth { get; set; }
                    public int? spreadingFactor { get; set; }
                    public string? codeRate { get; set; }
                    public bool? polarizationInversion { get; set; }
                }
            }
        }
    }
}
