using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace SerialDetector.Tests.Model.Deserializers
{
    internal class YamlDotNet
    {
        public void Foo(StringBuilder stringBuilder)
        {
            var deserializer = new Deserializer();
            deserializer.Deserialize<IDictionary<string, object>>(stringBuilder.ToString());
        }
    }
}