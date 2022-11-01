namespace LoraRouter
{
    public enum TxPowerMode
    {
        RANDOMIZE,
        MINIMUM,
        MAXIMUM,
        RSSI_MATCH
    }

    public class Gateway
    {
        public string? Id { get; set; }        
        public List<float> TxFrequencies { get; private set; } = new List<float>();
        public TxPowerMode PowerMode { get; set; } = TxPowerMode.MINIMUM;
        public uint MinPower { get; set; } = 12;
        public uint MaxPower { get; set; } = 20;

        internal uint GetPower(int rssi)
        {
            switch (PowerMode)
            {
                case TxPowerMode.MINIMUM:
                    return MinPower;

                case TxPowerMode.MAXIMUM:
                    return MaxPower;

                case TxPowerMode.RANDOMIZE:
                    return Utils.RandomUint(MinPower, MaxPower);

                case TxPowerMode.RSSI_MATCH:
                    throw new NotImplementedException();
            }

            throw new NotSupportedException();
        }

        internal bool CanTxRadio0(float freq)
        {
            return TxFrequencies.Contains(freq);
        }
    }
}
