using System.Collections.Generic;
using System.Linq;
using System.Text;
using cache_dns.Infrastructure;

namespace cache_dns
{
    public class Question
    {
        public readonly string Name;
        public readonly QueryType Type;
        public readonly QueryClass QueryClass;

        public Question(string name, QueryType type, QueryClass queryClass)
        {
            Name = name;
            Type = type;
            QueryClass = queryClass;
        }

        public override bool Equals(object obj)
        {
            return obj is Question other && 
                   Name == other.Name && 
                   Type.Code == other.Type.Code && 
                   QueryClass.Code == other.QueryClass.Code;
        }

        public static Question Parse(byte[] message, int start, out int next)
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
            next = index + 1 + 4;
            return new Question(name.ToString(), type, queryClass);
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
            return bytes;
        }
    }
}