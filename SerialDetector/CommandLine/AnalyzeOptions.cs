using CommandLine;

namespace SerialDetector.CommandLine
{
    [Verb("analyze", HelpText = "Analyze the .NET application against templates in the knowledge base")]
    internal class AnalyzeOptions
    {
        [Value(0, MetaName = "directory", Required = true, HelpText = "The directory of the analyzed application")]
        public string Directory { get; set; }
        
        [Option('o', "output", Required = true, HelpText = "The output directory for the results")]
        public string Output { get; set; }
    }
}