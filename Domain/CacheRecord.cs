using System;

namespace cache_dns.Domain
{
    public class CacheRecord
    {
        public readonly Record Record;
        public readonly DateTime DueTime;

        public CacheRecord(Record record)
        {
            Record = record;
            DueTime = DateTime.Now.AddSeconds(record.TimeToLive);
        }
    }
}