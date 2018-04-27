namespace cache_dns
{
    public class QueryType
    {
        public readonly short Code;
        private QueryType(short code) => Code = code;

        public static readonly QueryType A = new QueryType(1);
        public static readonly QueryType NS = new QueryType(2);
        public static readonly QueryType CNAME = new QueryType(5);
        public static readonly QueryType PTR = new QueryType(12);
        public static readonly QueryType HINFO = new QueryType(13);
        public static readonly QueryType MX = new QueryType(15);
        public static readonly QueryType AXFR = new QueryType(252);
        public static readonly QueryType Any = new QueryType(255);
        public static QueryType Parse(short value) => new QueryType(value);
    }
}