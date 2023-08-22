using PortfolioRisk.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioRisk.Core.DataTypes;

namespace RiskTerminal
{
    internal class RunConfig
    {
        #region Properties
        public double? TotalAllocation { get; set; }
        public List<string> Assets { get; set; }
        public List<string> AssetCurrencies { get; set; }
        public List<double> Weights { get; set; }
        public List<string> Factors { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        #endregion

        #region Accessor
        public Dictionary<string, AssetCurrency> CurrencyMapping =>
            Assets.Zip(AssetCurrencies).ToDictionary(p => p.First, p => p.Second);
        #endregion

        #region Helpers
        public bool ContainsMissingValue(out IEnumerable<string> missingAttributes)
        {
            // Automatically get all defined and thus required parameters for the configuration
            // and check whether any property is missing proper values
            missingAttributes = GetType()
                .GetProperties()
                .Where(p => (p.GetValue(this) ?? null) == null)
                .Select(p => p.Name);
            return GetType()
                .GetProperties()
                .Select(p => p.GetValue(this) ?? null)
                .Any(v => v == null);
        }
        public void NormalizeWeights()
        {
            double total = Weights.Sum();
            Weights = Weights.Select(w => w / total).ToList();
        }
        #endregion
    }

    class Program
    {
        private enum ArgumentParseMode
        {
            _Undetermined,
            TotalAllocation,
            Factors,
            Assets,
            AssetCurrencies,
            Weights,
            StartDate,
            EndDate
        }

        #region Entrance
        static void Main(string[] args)
        {
            // Wrap parsing commands with a try-catch to automatically handle some edge-cases of erroneous input formats
            try
            {
                ParseCommandsAndRun(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        static void ParseCommandsAndRun(string[] args)
        {
            if (args.Length == 0)
                PrintHelp();
            else if (args.First().ToLower() == "sample")
                RunSample();
            else
            {
                RunConfig parameters = new();

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
                        case "-c":
                            parseMode = ArgumentParseMode.AssetCurrencies;
                            parameters.AssetCurrencies = new List<string>();
                            break;
                        case "-w":
                            parseMode = ArgumentParseMode.Weights;
                            parameters.Weights = new List<double>();
                            break;
                        case "-f":
                            parseMode = ArgumentParseMode.Factors;
                            parameters.Factors = new List<string>();
                            break;
                        case "-s":
                            parseMode = ArgumentParseMode.StartDate;
                            break;
                        case "-e":
                            parseMode = ArgumentParseMode.EndDate;
                            break;
                        default:
                            switch (parseMode)
                            {
                                default:
                                case ArgumentParseMode._Undetermined:
                                    goto EndOfParsing;
                                case ArgumentParseMode.TotalAllocation:
                                    if (double.TryParse(arg.Replace(",", string.Empty), out double result))
                                        parameters.TotalAllocation = result;
                                    break;
                                case ArgumentParseMode.Assets:
                                    parameters.Assets.Add(arg.ToUpper());
                                    break;
                                case ArgumentParseMode.AssetCurrencies:
                                    parameters.AssetCurrencies.Add(arg.ToUpper());
                                    break;
                                case ArgumentParseMode.Factors:
                                    parameters.Factors.Add(arg.ToUpper());
                                    break;
                                case ArgumentParseMode.Weights:
                                    parameters.Weights.Add(double.Parse(arg));
                                    break;
                                case ArgumentParseMode.StartDate:
                                    parameters.StartDate = DateTime.Parse(arg);
                                    break;
                                case ArgumentParseMode.EndDate:
                                    parameters.EndDate = DateTime.Parse(arg);
                                    break;
                            }
                            break;
                    }
                }

            EndOfParsing: Run(parameters);
            }
        }
        #endregion

        #region Routines
        static void RunSample()
        {
            Console.WriteLine("Run sample data sets.");
            Run(new RunConfig()
            {
                Weights = new List<double> { 1, 1 },
                TotalAllocation = 2000000000,   // In CAD
                Assets = new List<string> { "SPY", "XIU" },
                AssetCurrencies = new List<string>{ "USD", "CAD" },
                Factors = new List<string> { "SPY", "XIU", "USD/CAD" },
                StartDate = new DateTime(2017, 1, 1),
                EndDate = new DateTime(2021, 12, 31)
            });
        }

        static void Run(RunConfig config)
        {
            // Check for missing inputs parameters
            if (config.ContainsMissingValue(out _))
            {
                Console.WriteLine("Invalid command line format.");
                return;
            }
            // Normalize weights
            config.NormalizeWeights();

            try
            {
                new PortfolioAnalyzer().Run(config);
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occured during analysis.");
                Console.WriteLine(e.Message);
            }
        }
        #endregion

        #region Helpers
        private static void PrintHelp()
        {
            Console.WriteLine(EmbeddedResourceHelper.ReadResource($"{nameof(RiskTerminal)}.README.md"));
        }
        #endregion
    }
}
