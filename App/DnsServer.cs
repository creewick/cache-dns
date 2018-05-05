using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using cache_dns.Domain;
using cache_dns.Domain.DnsMessage;
using Newtonsoft.Json;

namespace cache_dns.App
{
    public class DnsServer
    {
        // ReSharper disable InconsistentNaming
        private readonly Socket listenSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly Socket sendSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly byte[] buffer = new byte[1024];
        private readonly List<CacheRecord> answersCache;
        private readonly List<Question> questionsCache;
        private const string AnswersCacheName = "answersCache.txt";
        private const string QuestionsCacheName = "questionsCache.txt";
        
        public DnsServer(string address)
        {
            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 53));
            sendSocket.Connect(address, 53);
            sendSocket.ReceiveTimeout = 500;
            
            LoadCache(AnswersCacheName, out answersCache);
            LoadCache(QuestionsCacheName, out questionsCache);

            Console.CancelKeyPress += (s, e) => Dispose();
        }

        private void Dispose()
        {
            SaveCache(AnswersCacheName, answersCache);
            SaveCache(QuestionsCacheName, questionsCache);
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
                    EndPoint endPoint = new IPEndPoint(IPAddress.Any, 53);
                    listenSocket.ReceiveFrom(buffer, ref endPoint);
                    Console.WriteLine($"Received from {((IPEndPoint)endPoint).Address}");

                    var message = new DnsMessage(buffer);
                    var answer = GetAnswer(message);
                    
                    questionsCache.AddRange(message.Questions);
                    SaveCache(QuestionsCacheName, questionsCache);

                    if (answer != null)
                    {
                        listenSocket.SendTo(answer.GetBytes(), endPoint);
                        Console.WriteLine("Answer sent");
                    }
                    else
                        Console.WriteLine("Can't send answer");
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

        private DnsMessage GetAnswer(DnsMessage message)
        {
            if (TryGetAnswerFromCache(message, out var answer))
                return answer;
            if (TryGetQuestionFromCache(message, out answer))
                return answer;
            if (TryGetAnswerFromDns(message, out answer))
                return answer;
            return new DnsMessage(message.Id, MessageType.Query, RCode.Refused, 
                message.Questions, new List<Record>()); 
        }

        private bool TryGetAnswerFromCache(DnsMessage message, out DnsMessage answer)
        {
            var answers = new List<Record>();
            foreach (var question in message.Questions)
            {
                Console.WriteLine($"{question.Name} {question.Type.Code} {question.QueryClass.Code} requested");

                var found = false;
                foreach (var record in answersCache)
                {
                    if (!question.Equals(record)) continue;
                    answers.Add(record.Record);
                    found = true;
                }

                if (found) continue;
                answer = null;
                return false;
            }
            
            answer = new DnsMessage(message.Id, MessageType.Response, 
                RCode.Ok, message.Questions, answers);
            Console.WriteLine("Answer got from cache");
            return true;
        }     
        private bool TryGetQuestionFromCache(DnsMessage message, out DnsMessage answer)
        {
            foreach (var question in message.Questions)
            {
                var found = false;
                foreach (var cacheQuestion in questionsCache)
                    if (question.Equals(cacheQuestion))
                        found = true;

                if (found) continue;
                answer = null;
                return false;
            }
            answer = new DnsMessage(message.Id, MessageType.Query, RCode.Refused, 
                message.Questions, new List<Record>());
            Console.WriteLine("Question got from cache");
            return true;
        }   
        private bool TryGetAnswerFromDns(DnsMessage message, out DnsMessage answer)
        {
            
            sendSocket.Send(message.GetBytes());
            Console.WriteLine("Request sent");

            try
            {
                sendSocket.Receive(buffer, SocketFlags.None);
                Console.WriteLine("Answer received");
            }
            catch (SocketException)
            {
                answer = null;
                return false;
            }

            answer = new DnsMessage(buffer);
            AddToCache(answer, answersCache);
            SaveCache(AnswersCacheName, answersCache);
            return true;
        }

        private static void AddToCache(DnsMessage message, List<CacheRecord> cache)
        {
            cache.AddRange(message.Answers
                .Concat(message.Authorities)
                .Concat(message.Additionals)
                .Select(r => new CacheRecord(r)));
        }
        private static void LoadCache<T>(string filename, out List<T> cache)
        {
            cache = new List<T>();
            try
            {
                using (var sr = new StreamReader(filename))
                    cache = JsonConvert.DeserializeObject<List<T>>(sr.ReadToEnd());
                Console.WriteLine($"{filename} loaded");
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                SaveCache(filename, cache);
                Console.WriteLine($"New {filename} created");
            }
        }
        private static void SaveCache<T>(string filename, List<T> cache)
        {
            using (var sr = new StreamWriter(filename))
                sr.Write(JsonConvert.SerializeObject(cache));
        }
    }
}