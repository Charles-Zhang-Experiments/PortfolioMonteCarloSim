using Parcel.Shared.DataTypes;
using PortfolioRisk.Core.DataSourceService;
using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioRisk.Core.Algorithm;
using PortfolioRisk.Core.DataTypes;

namespace PortfolioRisk.Core
{
    public class PortfolioAnalyzer
    {
        #region State
        public Dictionary<string, List<TimeSeries>> TimeSeries { get; private set; }
        public List<Dictionary<string, double[]>> TotalReturns { get; private set; }
        #endregion

        #region Interface Function
        public void Run(AnalysisConfig config)
        {
            // Data preparation
            Stage1(config);

            // Simulate
            Stage2();

            // Report
            Stage3(config);
        }
        #endregion

        #region Web Interface
        public void Stage1(AnalysisConfig config)
        {
            // Normalize weights
            config.NormalizeWeights();

            // Fetch time series data
            Dictionary<string, DataGrid> originalTimeSeries = PopulateTimeSeries(config);

            // Yahoo Finance returns data with incomplete entries and sometimes wrong range of date;
            // So we need to pre-process and clean data
            // (unify dates, and fill in missing data for all weekdays, then we get matching number of rows)
            TimeSeries = CleanupData(originalTimeSeries);
        }
        public void Stage2()
        {
            // Perform the actual simulation and analysis
            HistoricalSimulation simulator = new HistoricalSimulation(TimeSeries);
            TotalReturns = Enumerable.Range(0, SimulationIterations)
                .AsParallel().Select(_ => simulator.SimulateOnce()).ToList();

            // Validation Assert
            if (TotalReturns.Count() != SimulationIterations ||
                TotalReturns.Any(r => r.Values.First().Count() != HistoricalSimulation.YearReturnDays))
                throw new InvalidOperationException("Unexpected simulation outcome.");
        }
        public Report Stage3(AnalysisConfig config)
        {
            // Reporting
            Reporter reporter = new Reporter(TotalReturns, GetCurrentPrices(config, out DateTime date), date);
            Report report = reporter.BuildReport(config, AnnotateAssetCurrency(config));
            reporter.AnnounceReport(config, report);

            return report;
        }
        #endregion

        #region Configurations
        public const int SimulationIterations = 5000;
        public const int AdjustedCloseColumnIndex = 5;
        public const int ETLWorstCaseTake = (int)(SimulationIterations * 0.1);
        #endregion

        #region Routines
        private Dictionary<string, DataGrid> PopulateTimeSeries(AnalysisConfig config)
        {
            Dictionary<string, DataGrid> timeSeries = new Dictionary<string, DataGrid>();
            foreach (string symbol in config.Assets.Union(config.Factors))
            {
                if (timeSeries.ContainsKey(symbol)) continue;
                DataGrid table = PreprocessAndFetchSymbol(symbol, config);
                if (table == null)
                    throw new ArgumentException("Failed to fetch data for series.");

                // Sort from past to present
                 table.Sort(table.Columns.First().Header, false);
                 timeSeries[symbol] = table;
            }

            return timeSeries;
        }
        private static Dictionary<string, double> GetCurrentPrices(AnalysisConfig config, out DateTime date)
        {
            DateTime priceDate = DateTime.Today;
            Dictionary<string, double> currentPrices =
                config.Assets.Union(config.Factors).ToDictionary(s => s, s => GetCurrentPrice(s, out priceDate));
            date = priceDate;
            return currentPrices;
        }
        private Dictionary<string, List<TimeSeries>> CleanupData(Dictionary<string, DataGrid> originalTimeSeries)
        {
            #region Preprocessing Logic
            // Intersect dates and find minimally shared range of date sequence
            DateTime[] weekDays = FindDateSequence();

            // Fill in missing data for all weekdays
            return FillMissingEntries();
            #endregion

            #region Local Functions
            DateTime[] FindDateSequence()
            {
                IEnumerable<DateTime>[] datesSeries =
                    originalTimeSeries.Values.Select(v => v.Columns.First().GetDataAs<DateTime>()).ToArray();
                DateTime[] intersection = datesSeries.Skip(1)
                    .Aggregate(new HashSet<DateTime>(datesSeries.First()), (h, e) =>
                    {
                        h.IntersectWith(e);
                        return h;
                    }).OrderBy(dt => dt).ToArray();
                return GetWorkDays(intersection.Min(), intersection.Max());
            }
            Dictionary<string, List<TimeSeries>> FillMissingEntries()
            {
                Dictionary<string, List<TimeSeries>> cleanData =
                    new Dictionary<string, List<TimeSeries>>();
                foreach ((string ticker, DataGrid table) in originalTimeSeries)
                {
                    // Extract existing time series
                    List<TimeSeries> timeSeries = GetTimeSeries(table);

                    // Back/Forward-fill if needed
                    PerformBackFill(timeSeries, ticker);

                    // Save result
                    cleanData.Add(ticker, timeSeries);
                }

                return cleanData;
            }
            static List<TimeSeries> GetTimeSeries(DataGrid table)
            {
                IEnumerable<DateTime> dateColumn = table.Columns.First().GetDataAs<DateTime>();
                IEnumerable<double> adjustedCloseColumn = table.Columns[AdjustedCloseColumnIndex].GetDataAs<double>();
                List<TimeSeries> timeSeries = dateColumn.Zip(adjustedCloseColumn)
                    .Select(tuple => new TimeSeries(tuple.First, tuple.Second)).ToList();
                return timeSeries;
            }
            void PerformBackFill(List<TimeSeries> timeSeries, string ticker)
            {
                foreach (DateTime weekday in weekDays)
                {
                    if (timeSeries.Any(ts => ts.Date == weekday))
                        continue;

                    Console.WriteLine($"{ticker} missing entry for date: {weekday:yyyy-MM-dd}"); // Report missing entries

                    if (FindSuitableFillinDate(timeSeries, weekday, out TimeSeries? substitute, out int index))
                    {
                        Console.WriteLine($"Back-fill with: {substitute!.Value.Date:yyyy-MM-dd}");
                        timeSeries.Insert(index, new TimeSeries(weekday, substitute!.Value.Value));
                    }
                    else throw new InvalidOperationException($"Cannot back-fill time series for {ticker}!");
                }
            }
            #endregion
        }
        
        private Dictionary<string, AssetCurrency> AnnotateAssetCurrency(AnalysisConfig config)
        {
            return config.Assets.Union(config.Factors).ToDictionary(s => s, GetCurrency);
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Automatically handle conversion of common names and interest rate
        /// </summary>
        private DataGrid PreprocessAndFetchSymbol(string originalSymbol, AnalysisConfig config)
        {
            SymbolDefinition symbol = new SymbolDefinition()
            {
                Name = originalSymbol,
                QueryStartDate = config.StartDate!.Value,
                QueryEndDate = config.EndDate!.Value
            };
            IDataSourceProvider provider = GetHandling(originalSymbol);
            return provider.GetSymbol(symbol);
        }
        
        private static double GetCurrentPrice(string symbol, out DateTime date)
        {
            SymbolDefinition query = new SymbolDefinition()
            {
                Name = symbol,
                QueryStartDate = DateTime.Now.Date.AddDays(-2),
                QueryEndDate = DateTime.Now.Date.AddDays(1)
            };
            DataGrid table = new YahooFinanceHelper().GetSymbol(query);

            // Get the latest
            table.Sort(table.Columns.First().Header, false);
            DataColumn column = table.Columns[AdjustedCloseColumnIndex];
            date = table.Columns[0][^1];
            return column[^1];
        }

        private AssetCurrency GetCurrency(string symbol)
        {
            return GetHandling(symbol).GetSymbolCurrency(symbol);
        }

        /// <summary>
        /// Get a sequence of all workdays between two end points
        /// </summary>
        private static DateTime[] GetWorkDays(DateTime start, DateTime end)
        {
            return Enumerable.Range(0, (end - start).Days + 1)
                .Select(i => start.AddDays(i))
                .Where(dt => dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday)
                .ToArray();
        }
        
        /// <summary>
        /// Given a target date (that's absent in a given time series) and a reference,
        /// find the most suitable entry to back-fill data for that date
        /// </summary>
        private bool FindSuitableFillinDate(List<TimeSeries> timeSeries, DateTime weekday, out TimeSeries? valueTuple, out int index)
        {
            // Strategy: First try to fill from a previously available date
            // If that is not available, try to fill from the next available date
            // If both fails, return false

            TimeSeries[] pastData = timeSeries.Where(ts => ts.Date <= weekday).ToArray();
            if (pastData.Count() != 0)
            {
                valueTuple = pastData.Last();
                index = timeSeries.FindIndex(ts => ts.Date == pastData.Last().Date) + 1;
                return true;
            }
            
            TimeSeries[] futureData = timeSeries.Where(ts => ts.Date >= weekday).ToArray();
            if (futureData.Count() != 0)
            {
                valueTuple = futureData.First();
                index = timeSeries.FindIndex(ts => ts.Date == futureData.First().Date);
                return true;
            }
            
            valueTuple = null;
            index = -1;
            return false;
        }

        private static IDataSourceProvider GetHandling(string symbol)
        {
            // Offline sources
            if (OfflineSourceHelper.OfflineSources.Contains(symbol))
            {
                return new OfflineSourceHelper();
            }
            // Interest rates
            else if (symbol.Contains('/'))
            {
                throw new NotImplementedException();
            }
            else return new YahooFinanceHelper();
        }
        #endregion
    }
}
