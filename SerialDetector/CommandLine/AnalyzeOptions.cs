using CommandLine;

namespace SerialDetector.CommandLine
{
    [Verb("analyze")]
    internal class AnalyzeOptions
    {
        [Value(0)]
        public string Directory { get; set; }
        
        [Option('o', "output", Default = "")]
        public string Output { get; set; }
    }
}