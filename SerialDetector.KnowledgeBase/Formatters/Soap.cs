using System.IO;
using System.Runtime.Serialization.Formatters.Soap;

namespace SerialDetector.KnowledgeBase.Formatters
{
    internal sealed class Soap : IFormatter
    {
        public Payload GeneratePayload(object gadget)
        {
            var soapFormatter = new SoapFormatter();
            var stream = new MemoryStream();
            soapFormatter.Serialize(stream, gadget);
            stream.Position = 0;
            return new Payload(stream.ToArray());
        }
    }
}