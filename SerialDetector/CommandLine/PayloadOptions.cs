using CommandLine;

namespace SerialDetector.CommandLine
{
    [Verb("payload", HelpText = "Generate a payload for insecure templates in the knowledge base")]
    internal class PayloadOptions
    {
        [Option('n', "name", HelpText = "The file name of the payload. Use either --name or the pair of --gadget and --formatter options")]
        public string Name { get; set; }
        
        [Option('g', "gadget", HelpText = "The gadget name. Use with --formatter")]
        public string Gadget { get; set; }
        
        [Option('f', "formatter", HelpText = "The formatter name to transform the gadget to the payload. Use with --gadget")]
        public string Formatter { get; set; }
        
        [Option('c', "command", Required = true, HelpText = "The command to be executed")]
        public string Command { get; set; }
        
        //[Option('t', "test", Default = false, HelpText = "Execute the command locally for testing purpose")]
        //public bool Test { get; set; }
    }
}