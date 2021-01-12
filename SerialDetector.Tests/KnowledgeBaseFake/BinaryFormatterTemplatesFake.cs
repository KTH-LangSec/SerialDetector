using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SerialDetector.KnowledgeBase;
using SerialDetector.KnowledgeBase.Formatters;

namespace SerialDetector.Tests.KnowledgeBaseFake
{
    internal class BinaryFormatterTemplatesFake: TemplateBase
    {
        public void OnlyPayloadGenerationCall()
        {
            var serializer = new BinaryFormatter();
            Template.CreateBySignature(it =>
                it.IsPayloadOf<TypeConfuseDelegateFake>().Format<Binary>());
        }
        
        public void DeserializeCall()
        {
            var serializer = new BinaryFormatter();
            Template.CreateBySignature(it =>
                serializer.Deserialize(
                    it.IsPayloadOf<TypeConfuseDelegateFake>().Format<Binary>().Cast<Stream>()));
        }
    }
}