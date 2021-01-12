using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SerialDetector.KnowledgeBase.Formatters
{
    internal class Binary : IFormatter
    {
        public Payload GeneratePayload(object gadget)
        {
            var binaryFormatter = new BinaryFormatter();
            var stream = new MemoryStream();
            binaryFormatter.Serialize(stream, gadget);
            stream.Position = 0;
            return new Payload(stream.ToArray());
        }
    }
}