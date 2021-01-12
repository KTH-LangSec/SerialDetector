using System.Windows.Markup;
using SerialDetector.KnowledgeBase.Formatters;
using SerialDetector.KnowledgeBase.Gadgets;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace SerialDetector.KnowledgeBase.Templates
{
    public class XamlMarkupTemplates : TemplateBase
    {
        public void XamlReaderLoadAsync()
        {
            var reader = new XamlReader();
            Template.CreateByName(it =>
                reader.LoadAsync(it.IsPayloadOf<ObjectDataProvider>().Format<Xaml>()));
        }
        
        public void XamlReaderLoad()
        {
            Template.CreateByName(it => 
                XamlReader.Load(it.IsPayloadOf<ObjectDataProvider>().Format<Xaml>()));
        }
        
        public void XmlReaderParse()
        {
            Template.CreateByName(it => 
                XamlReader.Parse(it.IsPayloadOf<ObjectDataProvider>().Format<Xaml>().Cast<string>()));
        }
    }
}