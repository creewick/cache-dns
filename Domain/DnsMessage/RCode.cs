namespace cache_dns
{
    public struct RCode
    {
        public readonly int Code;
        private RCode(int code) => Code = code;

        public static readonly RCode OK = new RCode(0);
        public static readonly RCode NameError = new RCode(3);
        public static RCode Parse(int value) => new RCode(value);
    }
}