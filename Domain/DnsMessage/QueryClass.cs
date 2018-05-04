using System;

namespace cache_dns.Domain.DnsMessage
{
    [Serializable]
    public class QueryClass
    {
        public readonly short Code;
        public QueryClass(short code) => Code = code;

        public static readonly QueryClass Internet = new QueryClass(1);
        public static QueryClass Parse(short value) => new QueryClass(value);
    }
}