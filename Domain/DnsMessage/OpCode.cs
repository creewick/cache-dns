namespace cache_dns
{
    public struct OpCode
    {
        public readonly int Code;
        private OpCode(int code) => Code = code;

        public static readonly OpCode Query = new OpCode(0);
        public static readonly OpCode InverseQuery = new OpCode(1);
        public static readonly OpCode Status = new OpCode(2);
        public static readonly OpCode Notify = new OpCode(4);
        public static readonly OpCode Update = new OpCode(5);
        public static OpCode Parse(int value) => new OpCode(value);
    }
}