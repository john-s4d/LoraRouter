namespace LoraRouter.ChirpStack
{
    public class CgbStatsPacket : IPacketStat
    {
        public string? gatewayID { get; set; }        
        public string? ip { get; set; }
        public DateTime? time { get; set; }
        public Location location { get; set; } = new Location();
        public string? configVersion { get; set; }
        public int? rxPacketsReceived { get; set; }
        public int? rxPacketsReceivedOK { get; set; }
        public int? txPacketsReceived { get; set; }
        public int? txPacketsEmitted { get; set; }

        public class Location
        {
            public float? latitude { get; set; }
            public float? longitude { get; set; }
            public int? altitude { get; set; }
            public string? source { get; set; }
        }
    }
}
