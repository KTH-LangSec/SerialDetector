using CommandLine;

namespace SerialDetector.CommandLine
{
    [Verb("payload")]
    internal class PayloadOptions
    {
        [Value(0)]
        public string Name { get; set; }
        
        [Option('c', "command", Required = true)]
        public string Command { get; set; }
        
        [Option('t', "test", Default = false)]
        public bool Test { get; set; }
    }
}