using System.Collections.Generic;
using System.Linq;
using System.Text;
using cache_dns.Infrastructure;

namespace cache_dns
{
    public class Record
    {
        public readonly string Name;
        public readonly QueryType Type;
        public readonly QueryClass QueryClass;
        public readonly int TimeToLive;
        public readonly short DataLength;
        public readonly byte[] Data;

        public Record(string name, QueryType type, QueryClass queryClass, int ttl, short length, byte[] data)
        {
            Name = name;
            Type = type;
            QueryClass = queryClass;
            TimeToLive = ttl;
            DataLength = length;
            Data = data;
        }        
        
        public static Record Parse(byte[] message, int start, out int next)
        {
            var index = start;
            var name = new StringBuilder();
            while (message[index] != 0)
            {
                name.Append(Convert.ToString(message
                    .Skip(index + 1)
                    .Take(message[index])));
                name.Append(".");
                index += message[index] + 1;
            }

            var type = QueryType.Parse(
                Convert.ToShort(message
                .Skip(index + 1)
                .Take(2)));
            var queryClass = QueryClass.Parse(
                Convert.ToShort(message
                .Skip(index + 3)
                .Take(2)));
            var timeToLive = Convert.ToInt(message
                .Skip(index + 5)
                .Take(4));
            var dataLength = Convert.ToShort(message
                .Skip(index + 9)
                .Take(2));
            var data = message
                .Skip(index + 11)
                .Take(dataLength)
                .ToArray();
            next = index + 1 + 10 + dataLength;
            return new Record(name.ToString(), type, queryClass, timeToLive, dataLength, data);
        }

        public IEnumerable<byte> GetBytes()
        {
            var bytes = new List<byte>();
            foreach (var part in Name.Split('.'))
            {
                var partAsBytes = Convert.GetBytes(part);
                bytes.Add((byte)partAsBytes.Count());
                bytes.AddRange(partAsBytes);
            }
            bytes.AddRange(Convert.GetBytes(Type.Code));
            bytes.AddRange(Convert.GetBytes(QueryClass.Code));
            bytes.AddRange(Convert.GetBytes(TimeToLive));
            bytes.AddRange(Convert.GetBytes(DataLength));
            bytes.AddRange(Data);
            return bytes;
        }
    }
}