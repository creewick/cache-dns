namespace cache_dns
{
    public class QueryClass
    {
        public readonly short Code;
        private QueryClass(short code) => Code = code;

        public static readonly QueryClass Internet = new QueryClass(1);
        public static QueryClass Parse(short value) => new QueryClass(value);
    }
}