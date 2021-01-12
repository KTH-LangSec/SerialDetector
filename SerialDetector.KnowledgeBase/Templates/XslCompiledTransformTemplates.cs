using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantCast

namespace SerialDetector.KnowledgeBase.Templates
{
    public class XslCompiledTransformTemplates : TemplateBase
    {
        public void XsltLoadWithPayload()
        {
            var xsl = new XslCompiledTransform();
            
            // load calls with enableScript == true
            Template.CreateByName(it => 
                xsl.Load(
                    it.IsPayloadFrom("MsxslScript.xsl").Cast<XmlReader>(), 
                    XsltSettings.TrustedXslt, 
                    null));
            
            var document = new XPathDocument(new StringReader("<?xml version='1.0'?><data></data>"));
            Template.CreateByName(it => xsl.Transform(document, XmlWriter.Create(TextWriter.Null)));
        }

        private void XsltLoad()
        {
            var xsl = new XslCompiledTransform();
            
            // load calls with enableScript == true
            xsl.Load((string)null, XsltSettings.TrustedXslt, null);
            xsl.Load((IXPathNavigable)null, XsltSettings.TrustedXslt, null);
            xsl.Load((XmlReader)null, XsltSettings.TrustedXslt, null);
        }

        private void XsltUsingSafe()
        {
            var xsl = new XslCompiledTransform();

            Template.CreateBySignature(it => xsl.Transform((string) null, (string) null));
            Template.CreateBySignature(it => xsl.Transform((string) null, (XmlWriter) null));
            Template.CreateBySignature(it => xsl.Transform((string) null, (XsltArgumentList) null, (Stream) null));
            Template.CreateBySignature(it => xsl.Transform((string) null, (XsltArgumentList) null, (TextWriter) null));
            Template.CreateBySignature(it => xsl.Transform((string) null, (XsltArgumentList) null, (XmlWriter) null));
            
            Template.CreateBySignature(it => xsl.Transform((XmlReader) null, (XmlWriter) null));
            Template.CreateBySignature(it => xsl.Transform((XmlReader) null, (XsltArgumentList) null, (Stream) null));
            Template.CreateBySignature(it => xsl.Transform((XmlReader) null, (XsltArgumentList) null, (TextWriter) null));
            Template.CreateBySignature(it => xsl.Transform((XmlReader) null, (XsltArgumentList) null, (XmlWriter) null));
            Template.CreateBySignature(it => xsl.Transform((XmlReader) null, (XsltArgumentList) null, (XmlWriter) null, null));
            
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XmlWriter) null));
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null, (Stream) null));
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null, (TextWriter) null));
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null, (XmlWriter) null));
            Template.CreateBySignature(it => xsl.Transform((IXPathNavigable) null, (XsltArgumentList) null, (XmlWriter) null, null));
        }
    }
}