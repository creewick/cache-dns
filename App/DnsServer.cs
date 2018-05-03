using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using cache_dns.Domain;
using Newtonsoft.Json;

namespace cache_dns
{
    public class DnsServer
    {
        private readonly Socket listenSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly Socket sendSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly List<CacheRecord> cache = new List<CacheRecord>();
        private readonly bool online;

        public DnsServer(string address, bool online)
        {
            this.online = online;
            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 53));
            sendSocket.Connect(address, 53);
            sendSocket.ReceiveTimeout = 2000;
            try
            {
                using (var sr = new StreamReader("cache.txt"))
                    cache = JsonConvert.DeserializeObject<List<CacheRecord>>(sr.ReadToEnd());
                Console.WriteLine("Cache loaded");
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                UpdateCache();
                Console.WriteLine("New cache file created");
            }

            Console.CancelKeyPress += Dispose;
        }

        private void Dispose(object sender, ConsoleCancelEventArgs consoleCancelEventArgs) => Dispose();
        private void Dispose()
        {
            UpdateCache();
            listenSocket.Close();
            sendSocket.Close();
            Console.WriteLine("Disposed the port");
        }
        
        public void Run()
        {
            try
            {
                while (true)
                {
                    HandleRequest();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"{e.Message} Error code: {e.ErrorCode}.");
            }
            finally
            {
                Dispose();
            }
        }

        private void UpdateCache()
        {
            using (var sr = new StreamWriter("cache.txt"))
                sr.Write(JsonConvert.SerializeObject(cache));
        }
        
        private void HandleRequest()
        {
            var buffer = new byte[1024];
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 53);
            listenSocket.ReceiveFrom(buffer, ref endPoint);
            Console.WriteLine($"Received from {((IPEndPoint)endPoint).Address}");

            var message = new DnsMessage(buffer);
            var answer = GetAnswer(message);

            if (answer != null)
            {
                listenSocket.SendTo(answer.GetBytes(), endPoint);
                Console.WriteLine("Answer sent");
            }
            else
                Console.WriteLine("Can't send answer");
        }

        private DnsMessage GetAnswer(DnsMessage message)
        {
            if (TryGetAnswerFromCache(message, out var answer))
                return answer;
            return !online ? null : GetAnswerFromDns(message);
        }

        private bool TryGetAnswerFromCache(DnsMessage message, out DnsMessage answer)
        {
            var answers = new List<Record>();
            foreach (var question in message.Questions)
            {
                Console.WriteLine($"{question.Name} {question.Type.Code} {question.QueryClass.Code} requested");

                var found = false;
                foreach (var record in cache)
                {
                    if (!question.Equals(record)) continue;
                    answers.Add(record.Record);
                    found = true;
                }
                
                if (found) continue;
                answer = null;
                return false;
            }
            answer = new DnsMessage(
                message.Id, MessageType.Response, message.OpCode, false, message.Questions, answers);
            Console.WriteLine("Answer got from cache");
            return true;
        }

        private DnsMessage GetAnswerFromDns(DnsMessage message)
        {
            sendSocket.Send(message.GetBytes());
            Console.WriteLine("Request sent");

            var buffer = new byte[1024];
            sendSocket.Receive(buffer, SocketFlags.None);
            Console.WriteLine("Answer received");
            
            var answer = new DnsMessage(buffer);
            SaveToCache(message, answer);
            UpdateCache();

            return answer;
        }
        
        private void SaveToCache(DnsMessage message, DnsMessage answer)
        {
            var records = answer.Answers
                .Concat(answer.Authorities)
                .Concat(answer.Additionals);
            foreach (var question in message.Questions)
            {
                if (!records.Any(record => question.Equals(record)))
                    records.Append(new Record(question.Name, question.Type, question.QueryClass, 20000, 0,
                        new byte[0]));
            }
            foreach (var record in records)
            {
                cache.Add(new CacheRecord(record));
                Console.WriteLine($"Saved to cache: {record.Name} {record.Type.Code} {record.QueryClass.Code}");
            }
        }
    }
}