using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Convert = cache_dns.Infrastructure.Convert;

namespace cache_dns.Domain.DnsMessage
{
    [Serializable]
    public class Question
    {
        public readonly string Name;
        public readonly QueryType Type;
        public readonly QueryClass QueryClass;
        
        // ReSharper disable once MemberCanBePrivate.Global
        public Question(string name, QueryType type, QueryClass queryClass)
        {
            Name = name;
            Type = type;
            QueryClass = queryClass;
        }

        public static Question Parse(byte[] message, int start, out int next)
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
            next = index + 1 + 4;
            return new Question(name.ToString(), type, queryClass);
        }

        public IEnumerable<byte> GetBytes()
        {
            var bytes = new List<byte>();
            foreach (var part in Name.Split('.'))
            {
                var partAsBytes = Convert.GetBytes(part);
                var list = partAsBytes.ToList();
                bytes.Add((byte)list.Count());
                bytes.AddRange(list);
            }
            bytes.AddRange(Convert.GetBytes(Type.Code));
            bytes.AddRange(Convert.GetBytes(QueryClass.Code));
            return bytes;
        }

        public bool Equals(CacheRecord record) => 
            Name == record.Record.Name &&
            QueryClass.Code == record.Record.QueryClass.Code &&
            Type.Code == record.Record.Type.Code;
        
        public bool Equals(Question other) => 
            Name == other.Name &&
            QueryClass.Code == other.QueryClass.Code &&
            Type.Code == other.Type.Code;
    }
}