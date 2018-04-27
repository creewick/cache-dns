namespace cache_dns
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new DnsServer().Run();
        }
    }
}