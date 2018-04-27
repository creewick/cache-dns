using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cache_dns.Infrastructure
{
    public static class Convert
    {
        public static int ToInt(IEnumerable<byte> bytes)
        {
            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse();
            return BitConverter.ToInt32(bytes.ToArray(), 0);
        }

        public static IEnumerable<byte> GetBytes(int number)
        {
            var bytes = BitConverter.GetBytes(number);
            return BitConverter.IsLittleEndian ? bytes.Reverse() : bytes;
        }
        
        public static short ToShort(IEnumerable<byte> bytes)
        {
            if (BitConverter.IsLittleEndian)
                bytes = bytes.Reverse();
            return BitConverter.ToInt16(bytes.ToArray(), 0);
        }
        
        public static IEnumerable<byte> GetBytes(short number)
        {
            var bytes = BitConverter.GetBytes(number);
            return BitConverter.IsLittleEndian ? bytes.Reverse() : bytes;
        }

        public static string ToString(IEnumerable<byte> bytes) => 
            Encoding.UTF8.GetString(bytes.ToArray());

        public static IEnumerable<byte> GetBytes(string text) =>
            Encoding.UTF8.GetBytes(text);
    }
}