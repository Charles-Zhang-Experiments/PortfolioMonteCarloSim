namespace ScenarioSimulator
{
    internal class RunConfig
    {
        public List<string> Inputs = new();
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                Console.WriteLine("""
                    ScenarioSimulator: Print help
                    ScenarioSimulator <Options List...> <Files Inputs List...>: Generate scenarios from inputs
                    """);
            else
            {
                try
                {
                    RunConfig config = Parse(args);
                    Run(config);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static RunConfig Parse(string[] args)
        {
            RunConfig config = new();

            string parsingSwitch = string.Empty;
            foreach (var arg in args)
            {
                switch (parsingSwitch)
                {
                    default:
                        if (arg.StartsWith('-'))
                            parsingSwitch = arg;
                        else if (File.Exists(arg))
                            Console.WriteLine($"Unrecognized argument or non-existing file: {arg}");
                        else
                            config.Inputs.Add(arg);
                        break;

                }
            }

            return config;
        }

        private static void Run(RunConfig config)
        {
            
        }
    }
}