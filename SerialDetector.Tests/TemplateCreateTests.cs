using System.Linq;
using SerialDetector.KnowledgeBase;
using SerialDetector.Tests.Model.Overloading;
using NUnit.Framework;
using SerialDetector.KnowledgeBase.Templates;

namespace SerialDetector.Tests
{
    // TODO: add tests for ref/out params, abstract class, interface
    public class TemplateCreateTests : TemplateBase
    {
        private Context context;
        
        [SetUp]
        public void Setup()
        {
            context = Context.CreateToAnalyze();
            Initialize(context);
        }
        
        [Test]
        public void CreateByNameOfProcedureOverloadedOnlyDeclaringClass()
        {
            var obj = new DerivedClass();
            Template.CreateByName(it => obj.Foo(5));

            var derivedName = $"{typeof(DerivedClass)}::{nameof(DerivedClass.Foo)}";
            var baseName = $"{typeof(BaseClass)}::{nameof(BaseClass.Foo)}";
            Assert.That(context.Templates.Select(info => info.Method.ToString()), Is.EquivalentTo(new []
            {
                $"{derivedName}()",
                $"{derivedName}(System.Int32)",
                $"{derivedName}(System.String)",
                $"{derivedName}(,System.Int32)",    // TODO: ignored generic parameter
                $"{derivedName}(System.Boolean,System.Object)",
                $"{derivedName}(System.Int32,System.Int32,System.Int32)",
                $"{baseName}()",
                $"{baseName}(System.Char)",
                $"{derivedName}(System.Double)",
                //$"{baseName}(System.Double)",    // TODO: ignored overloaded method
                $"{baseName}(System.Object)",    // it's Foo(dynamic d)
                
            }));
        }

        [Test]
        public void CreateByNameOfXslTransformTemplates()
        {
            var templateGroup = Loader.GetTemplateGroup(
                typeof(XslTransformTemplates),
                nameof(XslTransformTemplates.XsltLoadWithPayload));
            
            Assert.That(templateGroup.Name, Is.EqualTo(nameof(XslTransformTemplates)));
            //Assert.That(templateGroup.Templates.Count, Is.EqualTo(29));
            Assert.That(templateGroup.Templates.Select(info => info.Method.ToString()), Is.EquivalentTo(new []
            {
                "System.Xml.Xsl.XslTransform::Load(System.Xml.XmlReader)", 
                "System.Xml.Xsl.XslTransform::Load(System.Xml.XmlReader,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Load(System.Xml.XPath.IXPathNavigable)", 
                "System.Xml.Xsl.XslTransform::Load(System.Xml.XPath.IXPathNavigable,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Load(System.Xml.XPath.XPathNavigator)", 
                "System.Xml.Xsl.XslTransform::Load(System.Xml.XPath.XPathNavigator,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Load(System.String)", 
                "System.Xml.Xsl.XslTransform::Load(System.String,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Load(System.Xml.XPath.IXPathNavigable,System.Xml.XmlResolver,System.Security.Policy.Evidence)", 
                "System.Xml.Xsl.XslTransform::Load(System.Xml.XmlReader,System.Xml.XmlResolver,System.Security.Policy.Evidence)",
                "System.Xml.Xsl.XslTransform::Load(System.Xml.XPath.XPathNavigator,System.Xml.XmlResolver,System.Security.Policy.Evidence)", 
                
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.XPathNavigator,System.Xml.Xsl.XsltArgumentList,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.XPathNavigator,System.Xml.Xsl.XsltArgumentList)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.XPathNavigator,System.Xml.Xsl.XsltArgumentList,System.Xml.XmlWriter,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.XPathNavigator,System.Xml.Xsl.XsltArgumentList,System.Xml.XmlWriter)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.XPathNavigator,System.Xml.Xsl.XsltArgumentList,System.IO.Stream,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.XPathNavigator,System.Xml.Xsl.XsltArgumentList,System.IO.Stream)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.XPathNavigator,System.Xml.Xsl.XsltArgumentList,System.IO.TextWriter,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.XPathNavigator,System.Xml.Xsl.XsltArgumentList,System.IO.TextWriter)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.IXPathNavigable,System.Xml.Xsl.XsltArgumentList,System.Xml.XmlResolver)",
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.IXPathNavigable,System.Xml.Xsl.XsltArgumentList)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.IXPathNavigable,System.Xml.Xsl.XsltArgumentList,System.IO.TextWriter,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.IXPathNavigable,System.Xml.Xsl.XsltArgumentList,System.IO.TextWriter)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.IXPathNavigable,System.Xml.Xsl.XsltArgumentList,System.IO.Stream,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.IXPathNavigable,System.Xml.Xsl.XsltArgumentList,System.IO.Stream)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.IXPathNavigable,System.Xml.Xsl.XsltArgumentList,System.Xml.XmlWriter,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Transform(System.Xml.XPath.IXPathNavigable,System.Xml.Xsl.XsltArgumentList,System.Xml.XmlWriter)", 
                "System.Xml.Xsl.XslTransform::Transform(System.String,System.String,System.Xml.XmlResolver)", 
                "System.Xml.Xsl.XslTransform::Transform(System.String,System.String)"
            }));
        }
    }
}