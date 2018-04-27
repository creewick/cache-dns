using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using cache_dns.Domain;

namespace cache_dns
{
    public class DnsServer
    {
        private readonly Socket listenSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly Socket sendSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly Dictionary<Question, CacheRecord> cache = new Dictionary<Question, CacheRecord>();
        private readonly byte[] buffer = new byte[1024];

        public DnsServer()
        {
            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 53));
            sendSocket.Connect("8.8.8.8", 53);
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
                Console.WriteLine("{0} Error code: {1}.", e.Message, e.ErrorCode);
            }
            finally
            {
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
            Question key;
            foreach (var question in message.Questions)
            {
                Console.WriteLine($"{question.Name} requested"); 
                if (TryGetKey(question, out key))
                    answers.Add(cache[key].Record);
                else
                {
                    sendSocket.Send(buffer);
                    Console.WriteLine("Request sent");
                    sendSocket.Receive(buffer);
                    Console.WriteLine("Answer received");
                    SaveToCache(new DnsMessage(buffer).Answers);
                    listenSocket.SendTo(buffer, endPoint);
                    Console.WriteLine("Answer sent");
                    Console.WriteLine(cache.Count);
                    return;
                }
            }

            listenSocket.SendTo(
                new DnsMessage(message.Id, MessageType.Response, message.OpCode, false, 
                    message.QuestionCount, message.QuestionCount, message.Questions, answers).GetBytes()
                , endPoint);
            Console.WriteLine("Answer sent from cache");
        }

        private void SaveToCache(List<Record> records)
        {
            foreach (var record in records)
            {
                var question = new Question(record.Name, record.Type, record.QueryClass);
                Console.WriteLine($"{question.Name} saved");
                cache[question] = new CacheRecord(record);
            }
        }

        private bool TryGetKey(Question question, out Question result)
        {
            foreach (var key in cache.Keys)
                if (key.Name == question.Name && key.QueryClass == question.QueryClass && key.Type == question.Type)
                {
                    result = key;
                    return true;
                }

            result = null;
            return false;
        }
    }
}