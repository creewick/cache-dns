namespace cache_dns.Domain.DnsMessage
{
    public struct RCode
    {
        public readonly int Code;
        private RCode(int code) => Code = code;

        public static readonly RCode Ok = new RCode(0);
        public static readonly RCode NameError = new RCode(3);
        public static readonly RCode Refused = new RCode(5);
        public static RCode Parse(int value) => new RCode(value);
    }
}