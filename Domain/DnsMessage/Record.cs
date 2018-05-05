using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Convert = cache_dns.Infrastructure.Convert;

namespace cache_dns.Domain.DnsMessage
{    
    [Serializable]
    public class Record
    {
        // ReSharper disable MemberCanBePrivate.Global
        public readonly string Name;
        public readonly QueryType Type;
        public readonly QueryClass QueryClass;
        public readonly int TimeToLive;
        public readonly short DataLength;
        public readonly byte[] Data;

        public Record(string name, QueryType type, QueryClass queryClass, int ttl, byte[] data)
        {
            Name = name;
            Type = type;
            QueryClass = queryClass;
            TimeToLive = ttl;
            DataLength = (short)data.Length;
            Data = data;
        }        
        
        public static Record Parse(byte[] message, int start, out int next)
        {
            var index = start;
            var returnIndex = -1;
            var name = new StringBuilder();
            while (message[index] != 0)
            {
                if (message[index] >> 6 == 0b0000_0011)
                {
                    if (returnIndex == -1) returnIndex = index;
                    index = ((message[index] & 0b0011_1111) << 8) | message[index+1];
                    continue;
                }
                name.Append(Convert.ToString(message
                    .Skip(index + 1)
                    .Take(message[index])));
                name.Append(".");
                index += message[index] + 1;
            }

            if (returnIndex != -1) index = returnIndex + 1;
            var type = QueryType.Parse(
                Convert.ToShort(new[] {message[index+1], message[index+2]}));
            var queryClass = QueryClass.Parse(
                Convert.ToShort(new[] {message[index+3], message[index+4]}));
            var timeToLive = Convert.ToInt(new[] {message[index+5],message[index+6],message[index+7],message[index+8]});
            var dataLength = Convert.ToShort(new[] {message[index+9], message[index+10]});
            var data = message
                .Skip(index + 11)
                .Take(dataLength)
                .ToArray();
            next = index + 11 + dataLength;
            return new Record(name.ToString(), type, queryClass, timeToLive, data);
        }

        public IEnumerable<byte> GetBytes()
        {
            var bytes = new List<byte>();
            foreach (var part in Name.Split('.'))
            {
                var partAsBytes = Convert.GetBytes(part);
                var list = partAsBytes.ToList();
                bytes.Add((byte)list.Count());
                if (list.Any()) bytes.AddRange(list);
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