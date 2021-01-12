using System.Runtime.Serialization.Formatters.Binary;
using SerialDetector.KnowledgeBase.Formatters;
using SerialDetector.KnowledgeBase.Gadgets;
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace SerialDetector.KnowledgeBase.Templates
{
    public class BinaryFormatterTemplates : TemplateBase
    {
        public void Deserialize()
        {
            var serializer = new BinaryFormatter();
            Template.CreateBySignature(it =>
                serializer.Deserialize(
                    it.IsPayloadOf<TypeConfuseDelegate>().Format<Binary>()));
        }
        
        public void DeserializeHeaderHandler()
        {
            var serializer = new BinaryFormatter();
            Template.CreateBySignature(it =>
                serializer.Deserialize(
                    it.IsPayloadOf<TypeConfuseDelegate>().Format<Binary>(),
                    null));
        }
        
        public void DeserializeMethodResponse()
        {
            var serializer = new BinaryFormatter();
            Template.CreateBySignature(it =>
                serializer.DeserializeMethodResponse(
                    it.IsPayloadOf<TypeConfuseDelegate>().Format<Binary>(),
                    null,
                    null));
        }
        
        public void UnsafeDeserialize()
        {
            var serializer = new BinaryFormatter();
            Template.CreateBySignature(it =>
                serializer.UnsafeDeserialize(
                    it.IsPayloadOf<TypeConfuseDelegate>().Format<Binary>(),
                    null));
        }

        public void UnsafeDeserializeMethodResponse()
        {
            var serializer = new BinaryFormatter();
            Template.CreateBySignature(it =>
                serializer.UnsafeDeserializeMethodResponse(
                    it.IsPayloadOf<TypeConfuseDelegate>().Format<Binary>(),
                    null,
                    null));
        }
    }
}