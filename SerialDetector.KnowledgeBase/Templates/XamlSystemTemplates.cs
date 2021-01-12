using System.Xaml;
using SerialDetector.KnowledgeBase.Formatters;
using SerialDetector.KnowledgeBase.Gadgets;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace SerialDetector.KnowledgeBase.Templates
{
    public class XamlSystemTemplates : TemplateBase
    {
        public void XamlServicesLoad()
        {
            Template.CreateByName(it => 
                XamlServices.Load(it.IsPayloadOf<ObjectDataProvider>().Format<Xaml>()));
        }
        
        public void XamlServicesParse()
        {
            Template.CreateBySignature(it => 
                XamlServices.Parse(it.IsPayloadOf<ObjectDataProvider>().Format<Xaml>().Cast<string>()));
        }

        public void XamlServicesTransform()
        {
            Template.CreateByName(it => 
                XamlServices.Transform(
                    it.IsPayloadOf<ObjectDataProvider>().Format<Xaml>().Cast<XamlReader>(), 
                    new XamlObjectWriter(new XamlSchemaContext())));
        }

        private void XamlObjectWriterWhat()
        {
            // TODO: check XamlObjectWriter and other XamlWriters
            //var a = new XamlObjectWriter();
        }
    }
}