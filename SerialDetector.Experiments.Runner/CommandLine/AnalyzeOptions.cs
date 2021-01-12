using CommandLine;

namespace SerialDetector.Experiments.Runner.CommandLine
{
    [Verb("analyze")]
    internal class AnalyzeOptions
    {
        [Value(0)]
        public string Directory { get; set; }
        
        [Option('e', "entrypoint", Required = true)]
        public string EntryPoint { get; set; }
        
        [Option('o', "output", Default = "")]
        public string Output { get; set; }
    }
}