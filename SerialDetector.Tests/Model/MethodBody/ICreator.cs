using System.Text;

namespace SerialDetector.Tests.Model.MethodBody
{
    public interface ICreator
    {
        object CreateTaintedValue(string type);
    }
    
    internal sealed class StringBuilderCreatorA : ICreator
    {
        public object CreateTaintedValue(string type)
        {
            return new StringBuilder();
        }
    }
    
    internal sealed class StringBuilderCreatorB : ICreator
    {
        public object CreateTaintedValue(string type)
        {
            return type;
        }
    }
}