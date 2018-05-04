using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using cache_dns.Domain;
using cache_dns.Domain.DnsMessage;
using Newtonsoft.Json;

namespace cache_dns.App
{
    public class DnsServer
    {
        private readonly Socket listenSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly Socket sendSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        private readonly List<CacheRecord> answersCache = new List<CacheRecord>();
        private readonly List<Question> questionsCache = new List<Question>();
        private readonly bool online;

        public DnsServer(string address, bool online)
        {
            this.online = online;
            listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 53));
            sendSocket.Connect(address, 53);
            sendSocket.ReceiveTimeout = 500;
            try
            {
                using (var sr = new StreamReader("cacheAnswers.txt"))
                    answersCache = JsonConvert.DeserializeObject<List<CacheRecord>>(sr.ReadToEnd());
                Console.WriteLine("Answers cache loaded");
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                UpdateAnswersCache();
                Console.WriteLine("New cache file created");
            }
            try
            {
                using (var sr = new StreamReader("cacheQuestions.txt"))
                    questionsCache = JsonConvert.DeserializeObject<List<Question>>(sr.ReadToEnd());
                Console.WriteLine("Questions cache loaded");
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                UpdateQuestionsCache();
                Console.WriteLine("New cache file created");
            }

            Console.CancelKeyPress += Dispose;
        }

        private void Dispose(object sender, ConsoleCancelEventArgs consoleCancelEventArgs) => Dispose();
        private void Dispose()
        {
            UpdateAnswersCache();
            UpdateQuestionsCache();
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

        private void UpdateAnswersCache()
        {
            using (var sr = new StreamWriter("cacheAnswers.txt"))
                sr.Write(JsonConvert.SerializeObject(answersCache));
        }
        
        private void UpdateQuestionsCache()
        {
            using (var sr = new StreamWriter("cacheQuestions.txt"))
                sr.Write(JsonConvert.SerializeObject(questionsCache));
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
            if (TryGetQuestionFromCache(message, out answer))
                return answer;
            if (TryGetAnswerFromDns(message, out answer))
                return answer;
            return new DnsMessage(message.Id, MessageType.Query, OpCode.Query, 
                false, RCode.Refused, message.Questions, new List<Record>());

            
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
            answer = new DnsMessage(message.Id, MessageType.Query, OpCode.Query, 
                false, RCode.Refused, message.Questions, new List<Record>());
            return true;
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
            
            answer = new DnsMessage(message.Id, MessageType.Response, OpCode.Query, false, RCode.Ok, message.Questions, answers);
            Console.WriteLine("Answer got from cache");
            return true;
        }

        private bool TryGetAnswerFromDns(DnsMessage message, out DnsMessage answer)
        {
            
            sendSocket.Send(message.GetBytes());
            Console.WriteLine("Request sent");
            
            var buffer = new byte[1024];
            try
            {
                sendSocket.Receive(buffer, SocketFlags.None);
                Console.WriteLine("Answer received");
            }
            catch (SocketException e)
            {
                answer = null;
                return false;
            }

            answer = new DnsMessage(buffer);
            SaveToCaches(message, answer);
            UpdateAnswersCache();
            UpdateQuestionsCache();
            return true;
        }

        private void SaveToCaches(DnsMessage message, DnsMessage answer)
        {
            foreach (var question in message.Questions)
            {
                questionsCache.Add(question);
                Console.WriteLine(
                    $"Saved question to cache: {question.Name} {question.Type.Code} {question.QueryClass.Code}");
            }

            foreach (var record in answer.Answers
                .Concat(answer.Authorities)
                .Concat(answer.Additionals))
            {
                answersCache.Add(new CacheRecord(record));
                Console.WriteLine($"Saved answer to cache: {record.Name} {record.Type.Code} {record.QueryClass.Code}");
            }
        }
    }
}