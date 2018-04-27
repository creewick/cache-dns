namespace cache_dns
{
    public struct MessageType
    {
        public readonly bool Code;
        private MessageType(bool code) => Code = code;

        public static readonly MessageType Query = new MessageType(false);
        public static readonly MessageType Response = new MessageType(true);
        public static MessageType Parse(bool value) => new MessageType(value);
    }
}