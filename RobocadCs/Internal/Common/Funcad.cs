namespace RobocadCs.Internal.Common
{
    internal static class Funcad
    {
        public static bool AccessBit(byte data, int num)
        {
            int shift = num % 8;
            return ((data >> shift) & 0x1) == 1;
        }
    }
}
