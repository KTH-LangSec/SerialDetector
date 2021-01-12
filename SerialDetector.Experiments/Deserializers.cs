using System;
using System.IO;
using System.Runtime.Serialization.Json;
using Polenter.Serialization;
// ReSharper disable UnusedMember.Global

namespace SerialDetector.Experiments
{
    public class Deserializers
    {
        const string AllocateSensitiveSink = "System.RuntimeTypeHandle::Allocate(System.RuntimeType)";
        const string BinarySensitiveSink = "System.Runtime.Serialization.FormatterServices::nativeGetUninitializedObject(System.RuntimeType)";

        [SetUp(BinarySensitiveSink)]
        public void BinaryFormatter()
        {
            var serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            serializer.Deserialize(new MemoryStream());
        }
        
        [SetUp(AllocateSensitiveSink, 10)]
        public void DataContract()
        {
            var serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(object));
            serializer.ReadObject(new MemoryStream());
        }

        [SetUp(AllocateSensitiveSink, 10)]
        public void DataContractJson()
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(null);
            js.ReadObject(new MemoryStream());
        }

        [SetUp("fastJSON.Reflection/CreateObject::Invoke()")]
        public void FastJson()
        {
            fastJSON.JSON.ToObject("");     //new fastJSON.JSONParameters {BlackListTypeChecking = false}
        }

        [SetUp(AllocateSensitiveSink)]
        public void FsPickler()
        {
            var serializer = MBrace.CsPickler.CsPickler.CreateJsonSerializer(true);
            serializer.UnPickleOfString<Object>("");
        }

        [SetUp(AllocateSensitiveSink, 18)]
        public void JavaScript()
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            serializer.Deserialize<object>("");
        }

        [SetUp(BinarySensitiveSink)]
        public void Los()
        {
            var serializer = new System.Web.UI.LosFormatter();
            serializer.Deserialize(new MemoryStream());
        }

        [SetUp(AllocateSensitiveSink, 18)]
        public void NetDataContract()
        {
            var serializer = new System.Runtime.Serialization.NetDataContractSerializer();
            serializer.Deserialize(new MemoryStream());
        }

        [SetUp("Newtonsoft.Json.Serialization.ObjectConstructor`1::Invoke(System.Object[])")]
        public void NewtonsoftJson()
        {
            //new Newtonsoft.Json.JsonSerializerSettings() {TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All};
            Newtonsoft.Json.JsonConvert.DeserializeObject<object>("");
        }

        [SetUp(BinarySensitiveSink)]
        public void ObjectStateFormatter()
        {
            var serializer = new System.Web.UI.ObjectStateFormatter();
            serializer.Deserialize(new MemoryStream());
        }

        [SetUp(AllocateSensitiveSink)]
        public void SharpBinary()
        {
            SharpSerializer serializer = new SharpSerializer(true); // true -> binary
            serializer.Deserialize(new MemoryStream());
        }

        [SetUp(BinarySensitiveSink)]
        public void Soap()
        {
            var serializer = new System.Runtime.Serialization.Formatters.Soap.SoapFormatter();
            serializer.Deserialize(new MemoryStream());
        }

        [SetUp(AllocateSensitiveSink)]
        public void Xaml()
        {
            System.Windows.Markup.XamlReader.Load(new MemoryStream());
        }
        
        [SetUp(AllocateSensitiveSink)]
        public void Xml()
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(object));
            serializer.Deserialize(new MemoryStream());
        }

        [SetUp(AllocateSensitiveSink)]
        public void YamlDotNet()
        {
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            deserializer.Deserialize<object>("");
        }
    }
}