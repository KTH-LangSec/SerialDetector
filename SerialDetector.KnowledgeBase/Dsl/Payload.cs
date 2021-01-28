using System;
using System.IO;
using System.Text;

namespace SerialDetector.KnowledgeBase
{
    public class Payload
    {
        public static Payload FromFile(string fileName, string command)
        {
            var data = File.ReadAllText($@"Payloads\{fileName}");
            return new Payload(data.Replace("%CMD%", command));
       }
        
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

        public static implicit operator Stream(Payload payload) => payload.ToStream();
    }
}