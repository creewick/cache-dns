using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using cache_dns.Domain;
using Newtonsoft.Json;

namespace cache_dns
{
    public class DnsServer
    {
        private readonly Socket listenSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly Socket sendSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly List<CacheRecord> cache = new List<CacheRecord>();
        private readonly byte[] buffer = new byte[1024];

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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        
        public void Run()
        {
            try
            {
                while (true)
                {
                    HandleRequest();
                    var text = JsonConvert.SerializeObject(cache);
                    using (var sr = new StreamWriter("cache.txt"))
                        sr.Write(text);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("{0} Error code: {1}.", e.Message, e.ErrorCode);
            }
            finally
            {
                var text = JsonConvert.SerializeObject(cache);
                using (var sr = new StreamWriter("cache.txt"))
                    sr.Write(text);
                listenSocket.Close();
                Console.WriteLine("Disposed the port.");
            }
        }

        private void HandleRequest()
        {
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 53);
            listenSocket.ReceiveFrom(buffer, ref endPoint);
            Console.WriteLine("Received from {0}", ((IPEndPoint)endPoint).Address);

            var message = new DnsMessage(buffer);
            var answers = new List<Record>();
            foreach (var question in message.Questions)
            {
                Console.WriteLine($"{question.Name} requested");
                if (TryGetKey(question, out var index))
                    answers.Add(cache[index].Record);
                else
                {
                    sendSocket.Send(buffer);
                    Console.WriteLine("Request sent");
                    sendSocket.Receive(buffer);
                    Console.WriteLine("Answer received");
                    SaveToCache(question, new DnsMessage(buffer));
                    listenSocket.SendTo(buffer, endPoint);
                    Console.WriteLine("Answer sent");
                    return;
                }
            }
            listenSocket.SendTo(
                new DnsMessage(message.Id, MessageType.Response, message.OpCode, false, 
                               message.Questions, answers).GetBytes()
                , endPoint);
            Console.WriteLine("Answer sent from cache");
        }
        
        private bool TryGetKey(Question question, out int index)
        {
            for (var i = 0; i < cache.Count; i++)
                if (question.Name == cache[i].Record.Name &&
                    question.QueryClass.Code == cache[i].Record.QueryClass.Code &&
                    question.Type.Code == cache[i].Record.Type.Code)
                {
                    index = i;
                    return true;
                }

            index = -1;
            return false;
        }
        
        private void SaveToCache(Question question, DnsMessage message)
        {
            foreach (var record in 
                message.Answers
                .Concat(message.Additionals)
                .Concat(message.Authorities)
                .Concat(new List<Record>
                    {new Record(question.Name, question.Type, question.QueryClass, 
                        20000, 0, new byte[0])}))
            {
                cache.Add(new CacheRecord(record));
                Console.WriteLine(record.Name + " saved to cache");
            }
        }
    }
}