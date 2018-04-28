using System;
using System.Collections.Generic;
using System.Linq;
using Convert = cache_dns.Infrastructure.Convert;

namespace cache_dns
{
    public class DnsMessage
    {        
        public readonly short Id;
        public readonly MessageType QR;
        public readonly OpCode OpCode;
        public readonly bool AuthoritativeAnswer;
        public readonly bool Truncated;
        public readonly bool RecursionDesired;
        public readonly bool RecursionAvaliable;
        public readonly RCode RCode;
        public readonly short QuestionCount;
        public readonly short AnswerCount;
        public readonly short AuthorityCount;
        public readonly short AdditionalCount;

        public readonly List<Question> Questions = new List<Question>();
        public readonly List<Record> Answers = new List<Record>();
        public readonly List<Record> Authorities = new List<Record>();
        public readonly List<Record> Additionals = new List<Record>();

        public DnsMessage(short id, MessageType qr, OpCode opCode, bool authoritativeAnswer, 
            List<Question> questions, List<Record> answers)
        {
            Id = id;
            QR = qr;
            OpCode = opCode;
            AuthoritativeAnswer = authoritativeAnswer;
            Truncated = false;
            RecursionDesired = false;
            RecursionAvaliable = false;
            RCode = RCode.OK;
            QuestionCount = (short)questions.Count;
            AnswerCount = (short)answers.Count;
            AuthorityCount = 0;
            AdditionalCount = 0;
            Questions = questions;
            Answers = answers;
        }
        
        public DnsMessage(byte[] message)
        {
            Id = Convert.ToShort(message.Take(2));
            
            var thirdByte = message.Skip(2).First();
            QR = MessageType.Parse((thirdByte & 0b1000_0000) == 1);
            OpCode = OpCode.Parse((thirdByte & 0b0111_1000) >> 3);
            AuthoritativeAnswer = (thirdByte & 0b0000_0100) == 1;
            Truncated = (thirdByte & 0b0000_0010) == 1;
            RecursionDesired = (thirdByte & 0b0000_0001) == 1;

            var fourthByte = message.Skip(3).First();
            RecursionAvaliable = (fourthByte & 0b1000_0000) == 1;
            RCode = RCode.Parse(fourthByte & 0b0000_1111);

            QuestionCount = Convert.ToShort(message.Skip(4).Take(2));
            AnswerCount = Convert.ToShort(message.Skip(6).Take(2));
            AuthorityCount = Convert.ToShort(message.Skip(8).Take(2));
            AdditionalCount = Convert.ToShort(message.Skip(10).Take(2));

            var next = 12;
            for (var i = 0; i < QuestionCount; i++)
                Questions.Add(Question.Parse(message, next, out next));
            for (var i = 0; i < AnswerCount; i++)
                Answers.Add(Record.Parse(message, next, out next));
            for (var i = 0; i < AuthorityCount; i++)
                Authorities.Add(Record.Parse(message, next, out next));
            for (var i = 0; i < AdditionalCount; i++)
                Additionals.Add(Record.Parse(message, next, out next));
        }

        public byte[] GetBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(Convert.GetBytes(Id));
            bytes.Add((byte)((QR.Code ? 1 << 7 : 0 << 7) |
                             (OpCode.Code << 3) |
                             (AuthoritativeAnswer ? 1 << 2 : 0 << 2) |
                             (Truncated ? 1 << 1 : 0 << 1) |
                             (RecursionDesired ? 1 : 0)));
            bytes.Add((byte)((RecursionAvaliable ? 1 << 7 : 0 << 7) |
                              RCode.Code));
            bytes.AddRange(Convert.GetBytes(QuestionCount));
            bytes.AddRange(Convert.GetBytes(AnswerCount));
            bytes.AddRange(Convert.GetBytes(AuthorityCount));
            bytes.AddRange(Convert.GetBytes(AdditionalCount));
            foreach (var question in Questions)
                bytes.AddRange(question.GetBytes());
            foreach (var answer in Answers)
                bytes.AddRange(answer.GetBytes());
            foreach (var authority in Authorities)
                bytes.AddRange(authority.GetBytes());
            foreach (var additional in Additionals)
                bytes.AddRange(additional.GetBytes());
            return bytes.ToArray();
        }
    }
    
    
}