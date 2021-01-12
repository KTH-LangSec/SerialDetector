using System;
using System.IO;
using System.Text;

namespace SerialDetector.KnowledgeBase
{
    public class Payload
    {
        private readonly byte[] data;
        
        public Payload(byte[] data)
        {
            this.data = data;
        }

        public Payload(string data)
        {
            this.data = Encoding.UTF8.GetBytes(data);
        }

        public MemoryStream ToStream() => new MemoryStream(data);

        public override string ToString() => Encoding.UTF8.GetString(data);

        public string ToBase64String()
        {
            throw new NotImplementedException();
        }

        public static implicit operator Stream(Payload payload) => payload.ToStream();
    }
}