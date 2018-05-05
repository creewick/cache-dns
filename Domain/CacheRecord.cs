using System;
using cache_dns.Domain.DnsMessage;

namespace cache_dns.Domain
{
    [Serializable]
    public class CacheRecord
    {
        // ReSharper disable MemberCanBePrivate.Global
        public readonly Record Record;
        public readonly DateTime DueTime;

        public CacheRecord(Record record)
        {
            Record = record;
            DueTime = DateTime.Now.AddSeconds(record.TimeToLive);
        }
    }
}