using PortfolioRisk.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskTerminal
{
    internal enum ArgumentParseMode
    {
        _Undetermined,
        TotalAllocation,
        Factors,
        Assets,
        Weights
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Run sample for Question 1
            if (args.Length == 0 || args.First().ToLower() == "sample")
                RunSample();
            // Parse input parameters
            else
            {
                AnalysisConfig parameters = new AnalysisConfig();

                ArgumentParseMode parseMode = ArgumentParseMode._Undetermined;
                // Enumerate and parse the arguments using a state machine
                foreach (var arg in args)
                {
                    switch (arg.ToLower())
                    {
                        case "-t":
                            parseMode = ArgumentParseMode.TotalAllocation;
                            break;
                        case "-a":
                            parseMode = ArgumentParseMode.Assets;
                            parameters.Assets = new List<string>();
                            break;
                        case "-w":
                            parseMode = ArgumentParseMode.Weights;
                            parameters.Weights = new List<double>();
                            break;
                        case "-f":
                            parseMode = ArgumentParseMode.Factors;
                            parameters.Factors = new List<string>();
                            break;
                        default:
                            switch (parseMode)
                            {
                                case ArgumentParseMode._Undetermined:
                                    goto EndOfParsing;
                                case ArgumentParseMode.TotalAllocation:
                                    if(double.TryParse(arg.Replace(",", string.Empty), out double result))
                                        parameters.TotalAllocation = result;
                                    break;
                                case ArgumentParseMode.Assets:
                                    parameters.Assets.Add(arg.ToUpper());
                                    break;
                                case ArgumentParseMode.Factors:
                                    parameters.Factors.Add(arg.ToUpper());
                                    break;
                                case ArgumentParseMode.Weights:
                                    parameters.Weights.Add(double.Parse(arg));
                                    break;
                                default:
                                    break;
                            }
                            break;
                    }
                }

                EndOfParsing:
                if (new object[] { parameters.Assets, parameters.TotalAllocation, parameters.Weights }.Any(v => v == null))
                    Console.WriteLine("Invalid command line format.");
                else
                    Run(parameters);
            }
        }

        static void RunSample()
            => Run(new AnalysisConfig()
            {
                Weights = new List<double> { 1, 1 },
                TotalAllocation = 2000000000,
                Assets = new List<string> { "SPY", "XIU" },
                Factors = new List<string> { "SPY", "XIU", "USD/CAD" }
            });

        static void Run(AnalysisConfig config)
        {
            new PortfolioAnalyzer().Run(config);
        }
    }
}
