using System.IO;
using System.Text;
using System.Xml;
using SerialDetector.KnowledgeBase;
using NUnit.Framework;

namespace SerialDetector.Tests
{
    public class PayloadCastTests
    {
        [Test]
        public void CastString()
        {
            var payload = new Payload("TEST");
            Assert.That(payload.Cast<string>(), Is.EqualTo("TEST"));
        }

        [Test]
        public void CastTextReader()
        {
            var payload = new Payload("TEST");
            var reader = payload.Cast<TextReader>();
            Assert.That(reader.ReadToEnd(), Is.EqualTo("TEST"));
        }
        
        [Test]
        public void CastStringReader()
        {
            var payload = new Payload("TEST");
            var reader = payload.Cast<StringReader>();
            Assert.That(reader.ReadToEnd(), Is.EqualTo("TEST"));
        }
        
        [Test]
        public void CastXmlReader()
        {
            var xml = "<data></data>";
            var payload = new Payload(xml);
            var reader = payload.Cast<XmlReader>();

            reader.Read();
            Assert.That(reader.Name, Is.EqualTo("data"));
        }
    }
}