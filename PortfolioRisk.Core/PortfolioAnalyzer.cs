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
        #region Internal Data
        private Dictionary<string, DataGrid> OriginalTimeSeries { get; set; } = new Dictionary<string, DataGrid>();
        #endregion

        #region Interface Function
        public void Run(AnalysisConfig config)
        {
            // Fetch time series data
            PopulateTimeSeries(config);

            // Yahoo Finance returns data with incomplete entries and sometimes wrong range of date;
            // So we need to pre-process and clean data
            // (unify dates, and fill in missing data for all weekdays, then we get matching number of rows)
            Dictionary<string, List<TimeSeries>> timeSeries = CleanupData();

            // Perform the actual simulation and analysis
            HistoricalSimulation simulator = new HistoricalSimulation(timeSeries);
            List<Dictionary<string, double[]>> results = Enumerable.Range(0, SimulationIterations)
                .AsParallel().Select(_ => simulator.SimulateOnce()).ToList();
            
            // Validation Asserts
            if (results.Count() != SimulationIterations ||
                results.Any(r => r.Values.First().Count() != HistoricalSimulation.YearReturnDays))
                throw new InvalidOperationException("Unexpected simulation outcome.");
            
            // Reporting
            var reporter = new Reporter(results, GetCurrentPrices(config));
            Report report = reporter.BuildReport(config, AnnotateAssetCurrency(config));
            reporter.AnnounceReport(report);
        }
        #endregion

        #region Configurations
        public const int SimulationIterations = 5000;
        public const int AdjustedCloseColumnIndex = 5;
        public const int ETLWorstCaseTake = (int)(SimulationIterations * 0.1);
        #endregion

        #region Routines
        private void PopulateTimeSeries(AnalysisConfig config)
        {
            foreach (string symbol in config.Assets.Union(config.Factors))
            {
                if (OriginalTimeSeries.ContainsKey(symbol)) continue;
                DataGrid table = PreprocessAndFetchSymbol(symbol, config);
                if (table == null)
                    throw new ArgumentException("Failed to fetch data for series.");

                // Sort from past to present
                table.Sort(table.Columns.First().Header, false);
                OriginalTimeSeries[symbol] = table;
            }
        }
        private Dictionary<string, double> GetCurrentPrices(AnalysisConfig config)
        {
            Dictionary<string, double> currentPrices =
                config.Assets.Union(config.Factors).ToDictionary(s => s, GetCurrentPrice);
            return currentPrices;
        }
        private Dictionary<string, List<TimeSeries>> CleanupData()
        {
            // Intersect dates and find minimally shared range of date sequence
            IEnumerable<DateTime>[] datesSeries =
                OriginalTimeSeries.Values.Select(v => v.Columns.First().GetDataAs<DateTime>()).ToArray();
            DateTime[] intersection = datesSeries.Skip(1)
                .Aggregate(new HashSet<DateTime>(datesSeries.First()), (h, e) =>
                {
                    h.IntersectWith(e);
                    return h;
                }).OrderBy(dt => dt).ToArray();
            DateTime[] weekDays = GetWorkDays(intersection.Min(), intersection.Max());
            
            // Fill in missing data for all weekdays
            Dictionary<string, List<TimeSeries>> cleanData =
                new Dictionary<string, List<TimeSeries>>();
            foreach (KeyValuePair<string, DataGrid> pair in OriginalTimeSeries)
            {
                // Extract time series
                string ticker = pair.Key;
                DataGrid table = pair.Value;
                var dateColumn = table.Columns.First().GetDataAs<DateTime>();
                var adjustedCloseColumn = table.Columns[AdjustedCloseColumnIndex].GetDataAs<double>();
                List<TimeSeries> timeSeries = dateColumn.Zip(adjustedCloseColumn).Select(tuple => new TimeSeries(tuple.First, tuple.Second)).ToList();

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
                
                // Save result
                cleanData.Add(ticker, timeSeries);
            }

            return cleanData;
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
        
        private double GetCurrentPrice(string symbol)
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

        private IDataSourceProvider GetHandling(string symbol)
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
