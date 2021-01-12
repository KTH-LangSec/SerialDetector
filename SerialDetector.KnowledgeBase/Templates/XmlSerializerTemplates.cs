using System.IO;
using System.Xml.Serialization;
using SerialDetector.KnowledgeBase.Formatters;
using SerialDetector.KnowledgeBase.Gadgets;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace SerialDetector.KnowledgeBase.Templates
{
    public sealed class XmlSerializerTemplates : TemplateBase
    {
        public void Deserialize()
        {
            var rootType = typeof(
                System.Data.Services.Internal.ExpandedWrapper<
                    System.Windows.Markup.XamlReader,
                    System.Windows.Data.ObjectDataProvider
                >);
            
            var serializer = new XmlSerializer(rootType);
            Template.CreateBySignature(it =>
                serializer.Deserialize(
                    it.IsPayloadFrom("ObjectDataProvider.xml").Cast<TextReader>()));
        }
    }
}