using System.Text;

namespace LoraRouter
{
    public static class Utils
    {
        public static string ToHexString(byte[] bytes, int index, int count)
        {
            var result = new StringBuilder(bytes.Length * 2);

            for (int i = index; i < index + count; i++)
            {
                result.AppendFormat("{0:x2}", bytes[i]);
            }
            return result.ToString();
        }

        public static string ToHexString(byte[] bytes)
        {
            return ToHexString(bytes, 0, bytes.Length);
        }

        public static byte[] FromHexString(string input)
        {
            var result = new byte[input.Length / 2];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = System.Convert.ToByte(input.Substring(i * 2, 2), 16);
            }
            return result;
        }

        public static uint RandomUint(uint min, uint max)
        {
            return (BitConverter.ToUInt32(RandomBytes(sizeof(uint)), 0) % (max - min)) + min;
        }

        public static uint RandomUint()                        
        {
            return RandomUint(uint.MinValue, uint.MaxValue);
        }

        public static byte[] RandomBytes(int length)
        {
            byte[] rand = new byte[length];
            new Random().NextBytes(rand);
            return rand;
        }
    }
}
