using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantCast

// Suppress the warning of using obsolete type XslTransform
#pragma warning disable 618

namespace SerialDetector.KnowledgeBase.Templates
{
    public class XslTransformTemplates : TemplateBase
    {
        public void XsltLoadWithPayload()
        {
            var xsl = new XslTransform();
            Template.CreateByName(it => xsl.Load(it.IsPayloadFrom("MsxslScript.xsl").Cast<XmlReader>()));
            
            var document = new XPathDocument(new StringReader("<?xml version='1.0'?><data></data>"));
            Template.CreateByName(it => xsl.Transform(document, null, TextWriter.Null, null));
        }

        private void XsltLoad()
        {
            var xsl = new XslTransform();
            
            Template.CreateBySignature(it => xsl.Load((string)null));            
            Template.CreateBySignature(it => xsl.Load((string)null, null));
            
            //Template.CreateBySignature(it => xsl.Load((XmlReader) null));
            Template.CreateBySignature(it => xsl.Load((XmlReader) null, null));
            Template.CreateBySignature(it => xsl.Load((XmlReader) null, null, null));
            
            Template.CreateBySignature(it => xsl.Load((IXPathNavigable) null));
            Template.CreateBySignature(it => xsl.Load((IXPathNavigable) null, null));
            Template.CreateBySignature(it => xsl.Load((IXPathNavigable) null, null, null));
            
            Template.CreateBySignature(it => xsl.Load((XPathNavigator) null));
            Template.CreateBySignature(it => xsl.Load((XPathNavigator) null, null));
            Template.CreateBySignature(it => xsl.Load((XPathNavigator) null, null, null));
        }

        private void XsltTransform()
        {
            var xsl = new XslTransform();

            Template.CreateBySignature(it => xsl.Transform((string) null, (string) null));
            Template.CreateBySignature(it => xsl.Transform((string) null, (string) null, null));
            
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null));
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null, (XmlResolver) null));
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null, (Stream) null));
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null, (Stream) null, null));
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null, (TextWriter) null));
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null, (TextWriter) null, null));
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null, (XmlWriter) null));
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null, (XmlWriter) null, null));

            Template.CreateBySignature(it => xsl.Transform((XPathNavigator) null, (XsltArgumentList) null));
            Template.CreateBySignature(it => xsl.Transform((XPathNavigator) null, (XsltArgumentList) null, (XmlResolver) null));
            Template.CreateBySignature(it => xsl.Transform((XPathNavigator) null, (XsltArgumentList) null, (Stream) null));
            Template.CreateBySignature(it => xsl.Transform((XPathNavigator) null, (XsltArgumentList) null, (Stream) null, null));
            Template.CreateBySignature(it => xsl.Transform((XPathNavigator) null, (XsltArgumentList) null, (TextWriter) null));
            Template.CreateBySignature(it => xsl.Transform((XPathNavigator) null, (XsltArgumentList) null, (TextWriter) null, null));
            Template.CreateBySignature(it => xsl.Transform((XPathNavigator) null, (XsltArgumentList) null, (XmlWriter) null));
            Template.CreateBySignature(it => xsl.Transform((XPathNavigator) null, (XsltArgumentList) null, (XmlWriter) null, null));
        }
    }
}