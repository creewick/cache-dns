using System;

namespace cache_dns.Domain.DnsMessage
{    
    [Serializable]
    public class QueryType
    {
        public readonly short Code;
        public QueryType(short code) => Code = code;

        public static readonly QueryType A = new QueryType(1);
        public static readonly QueryType Ns = new QueryType(2);
        public static readonly QueryType Cname = new QueryType(5);
        public static readonly QueryType Ptr = new QueryType(12);
        public static readonly QueryType Hinfo = new QueryType(13);
        public static readonly QueryType Mx = new QueryType(15);
        public static readonly QueryType Axfr = new QueryType(252);
        public static readonly QueryType Any = new QueryType(255);
        public static QueryType Parse(short value) => new QueryType(value);
    }
}