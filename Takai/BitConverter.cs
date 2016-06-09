using System;

namespace Takai
{
    /// <summary>
    /// Convert to a char or int32 from a byte array that is little endian
    /// </summary>
    internal static class BitConverter
    {
        public static char ToChar(byte[] value, int startIndex)
        {
            if (System.BitConverter.IsLittleEndian)
                return System.BitConverter.ToChar(value, startIndex);
            else
            {
                Array.Reverse(value);
                return System.BitConverter.ToChar(value, startIndex);
            }
        }

        public static int ToInt32(byte[] value, int startIndex)
        {
            if (System.BitConverter.IsLittleEndian)
                return System.BitConverter.ToInt32(value, startIndex);
            else
            {
                Array.Reverse(value);
                return System.BitConverter.ToInt32(value, startIndex);
            }
        }

        public static long ToInt64(byte[] value, int startIndex)
        {
            if (System.BitConverter.IsLittleEndian)
                return System.BitConverter.ToInt64(value, startIndex);
            else
            {
                Array.Reverse(value);
                return System.BitConverter.ToInt64(value, startIndex);
            }
        }
    }
}