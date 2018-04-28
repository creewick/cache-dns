using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        public DnsServer(string address)
        {
            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 53));
            sendSocket.Connect(address, 53);
            sendSocket.ReceiveTimeout = 2000;
            try
            {
                using (var sr = new StreamReader("cache.txt"))
                {
                    cache = JsonConvert.DeserializeObject<List<CacheRecord>>(sr.ReadToEnd());
                    Console.WriteLine("Cache loaded");
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                UpdateCache();
                Console.WriteLine("New cache file created");
            }
        }
        
        public void Run()
        {
            try
            {
                while (!Console.KeyAvailable)
                {
                    HandleRequest();
                    UpdateCache();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"{e.Message} Error code: {e.ErrorCode}.");
            }
            finally
            {
                UpdateCache();
                listenSocket.Close();
                sendSocket.Close();
                Console.WriteLine("Disposed the port");
            }
        }

        private void UpdateCache()
        {
            using (var sr = new StreamWriter("cache.txt"))
                sr.Write(JsonConvert.SerializeObject(cache));
        }
        
        private async void HandleRequest()
        {
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 53);
            var buffer = new byte[1024];
            await new Task(() => listenSocket.ReceiveFrom(buffer, ref endPoint));
            Console.WriteLine($"Received from {0}", ((IPEndPoint)endPoint).Address);

            var message = new DnsMessage(buffer.ToArray());
            var answers = new List<Record>();
            foreach (var question in message.Questions)
            {
                Console.WriteLine($"{question.Name} requested");

                var index = GetIndex(question);
                if (index != -1)
                    answers.Add(cache[index].Record);
                else
                {
                    sendSocket.Send(buffer.ToArray());
                    Console.WriteLine("Request sent");
                    
                    await sendSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    Console.WriteLine("Answer received");
                    
                    SaveToCache(question, new DnsMessage(buffer.ToArray()));
                    
                    listenSocket.SendTo(buffer.ToArray(), endPoint);
                    Console.WriteLine("Answer sent");
                    return;
                }
            }

            listenSocket.SendTo(
                new DnsMessage(message.Id, MessageType.Response, message.OpCode, false, message.Questions, answers)
                    .GetBytes(), endPoint);
            Console.WriteLine("Answer sent from cache");
        }
        
        private int GetIndex(Question question)
        {
            for (var i = 0; i < cache.Count; i++)
                if (question.Equals(cache[i]))
                    return i;
            return -1;
        }
        
        private void SaveToCache(Question question, DnsMessage message)
        {
            var records = message.Answers
                .Concat(message.Additionals)
                .Concat(message.Authorities)
                .Concat(new List<Record>
                    {new Record(question.Name, question.Type, question.QueryClass, 20000, 0, new byte[0])});
            
            foreach (var record in records)
            {
                cache.Add(new CacheRecord(record));
                Console.WriteLine(record.Name + " saved to cache");
            }
        }
    }
}