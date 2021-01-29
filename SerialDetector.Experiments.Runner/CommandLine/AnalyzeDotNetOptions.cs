using CommandLine;

namespace SerialDetector.Experiments.Runner.CommandLine
{
    [Verb("analyze-dotnet", HelpText = "Analyze .NET Framework for OIV patterns detection")]
    internal sealed class AnalyzeDotNetOptions
    {
        [Value(0, MetaName = "directory", Required = false, HelpText = "The optional directory with 3rd party libs to analyze them with .NET Framework")]
        public string Directory { get; set; }

        [Option('t', "temp", Required = true, HelpText = "The temporary directory to copy files of the current .NET Framework and 3rd party libs")]
        public string TempDirectory { get; set; }

        [Option('e', "entrypoint", Required = true, HelpText = "The entry point name from the assembly SerialDetector.Experiments")]
        public string EntryPoint { get; set; }

        [Option('o', "output", Required = true, HelpText = "The output directory for the results")]
        public string Output { get; set; }
    }
}