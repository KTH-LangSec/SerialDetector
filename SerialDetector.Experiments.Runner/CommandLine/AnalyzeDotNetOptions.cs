using CommandLine;

namespace SerialDetector.Experiments.Runner.CommandLine
{
    [Verb("analyze-dotnet")]
    internal sealed class AnalyzeDotNetOptions
    {
        [Value(0, Required = false)]
        public string Directory { get; set; }

        [Option('t', "temp", Required = true)]
        public string TempDirectory { get; set; }

        [Option('e', "entrypoint", Required = true)]
        public string EntryPoint { get; set; }

        [Option('o', "output", Default = "")]
        public string Output { get; set; }
    }
}