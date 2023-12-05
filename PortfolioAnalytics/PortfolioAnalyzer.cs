using PortfolioRisk.Core.DataTypes;
using PortfolioAnalytics;

namespace PortfolioRisk.Core
{
    public record FactorReturn(string Name, double[] PnLs);
    public class PortfolioAnalyzer
    {
        #region State
        public TimeSeries[] TimeSeries { get; private set; }
        public List<Dictionary<string, double[]>> TotalReturns { get; private set; }
        #endregion

        #region Interface Function
        public void Run(PortfolioConfiguration config)
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
        public void Stage1(PortfolioConfiguration config)
        {
            // Normalize weights
            config.NormalizeWeights();

            // Fetch time series data
            TimeSeries[] originalTimeSeries = PopulateTimeSeries(config);

            // Yahoo Finance returns data with incomplete entries and sometimes wrong range of date;
            // So we need to pre-process and clean data
            // (unify dates, and fill in missing data for all weekdays, then we get matching number of rows)
            TimeSeries = CleanupData(originalTimeSeries);
        }
        public void Stage2()
        {
            // Perform the actual simulation and analysis
            BatchHistoricalSimulation simulator = new BatchHistoricalSimulation(TimeSeries);
            TotalReturns = Enumerable.Range(0, SimulationIterations)
                .AsParallel().Select(_ => simulator.SimulateOnce()).ToList();

            // Validation Assert
            if (TotalReturns.Count() != SimulationIterations ||
                TotalReturns.Any(r => r.Values.First().Count() != BatchHistoricalSimulation.YearReturnDays))
                throw new InvalidOperationException("Unexpected simulation outcome.");
        }
        public Report Stage3(PortfolioConfiguration config)
        {
            // Reporting
            Reporter reporter = new Reporter(TotalReturns, GetCurrentPrices(config, out DateTime date), date);
            Report report = reporter.BuildReport(config, AnnotateAssetCurrency(config), this);
            reporter.AnnounceReport(config, report);

            return report;
        }
        #endregion

        #region Configurations
        public const int SimulationIterations = 5000;
        public const int ETLWorstCaseTake = (int)(SimulationIterations * 0.1);
        #endregion

        #region Routines
        private static TimeSeries[] PopulateTimeSeries(PortfolioConfiguration config)
        {
            Dictionary<string, TimeSeries> timeSeries = new();
            foreach (string symbol in config.AllFactors)
            {
                if (timeSeries.ContainsKey(symbol)) continue;
                TimeSeries series = new YahooFinanceHelper().GetSymbol(new SymbolDefinition(symbol, config.StartDate!.Value, config.EndDate!.Value, null), YahooFinanceHelper.YahooTimeInterval.OneDay);
                if (series == null)
                    throw new ArgumentException("Failed to fetch data for series.");

                // Sort from past to present
                table.Sort(table.Columns.First().Header, false);
                timeSeries[symbol] = series;
            }

            return timeSeries.Values.ToArray();
        }
        private static Dictionary<string, double> GetCurrentPrices(PortfolioConfiguration config, out DateTime date)
        {
            DateTime priceDate = DateTime.Today;
            Dictionary<string, double> currentPrices =
                config.AllFactors.ToDictionary(s => s, s => GetCurrentPrice(s, null).Value);
            date = priceDate;
            return currentPrices;
        }
        private static TimeSeries[] CleanupData(TimeSeries[] originalTimeSeries)
        {
            // Intersect dates and find minimally shared range of date sequence
            DateTime[] weekDays = FindDateSequence(originalTimeSeries);

            // Fill in missing data for all weekdays, Back/Forward-fill if needed
            return originalTimeSeries.Select(ts => PerformBackFill(ts, weekDays)).ToArray();
        }
        #endregion

        #region Helpers
        private static DateTime[] FindDateSequence(TimeSeries[] originalTimeSeries)
        {
            IEnumerable<DateTime>[] datesSeries =
                originalTimeSeries.Select(v => v.DataPoints.Select(p => p.Date)).ToArray();
            DateTime[] intersection = datesSeries.Skip(1)
                .Aggregate(new HashSet<DateTime>(datesSeries.First()), (h, e) =>
                {
                    h.IntersectWith(e);
                    return h;
                }).OrderBy(dt => dt).ToArray();
            return GetWorkDays(intersection.Min(), intersection.Max());
        }
        private static TimeSeries PerformBackFill(in TimeSeries timeSeries, DateTime[] weekDays)
        {
            var modified = timeSeries.DataPoints.ToList();
            foreach (DateTime weekday in weekDays)
            {
                if (modified.Any(ts => ts.Date == weekday))
                    continue;

                Console.WriteLine($"{timeSeries.Name} missing entry for date: {weekday:yyyy-MM-dd}"); // Report missing entries

                if (FindSuitableFillinDate(modified, weekday, out TimePoint? substitute, out int index))
                {
                    Console.WriteLine($"Back-fill with: {substitute!.Value.Date:yyyy-MM-dd}");
                    modified.Insert(index, new TimePoint(weekday, substitute!.Value.Value));
                }
                else throw new InvalidOperationException($"Cannot back-fill time series for {timeSeries.Name}!");
            }
            return new TimeSeries()
            {
                Name = timeSeries.Name,
                DataPoints = modified.ToArray(),
                Symbol = timeSeries.Symbol,
            };
        }
        private static TimePoint GetCurrentPrice(string symbol, string currency)
        {
            SymbolDefinition query = new SymbolDefinition(symbol, DateTime.Now.Date.AddDays(-2), DateTime.Now.Date.AddDays(1), currency);
            TimeSeries series = new YahooFinanceHelper().GetSymbol(query, YahooFinanceHelper.YahooTimeInterval.OneDay);

            // Get the latest
            return series.DataPoints.Last();
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
        private static bool FindSuitableFillinDate(List<TimePoint> timeSeries, DateTime weekday, out TimePoint? valueTuple, out int index)
        {
            // Strategy: First try to fill from a previously available date
            // If that is not available, try to fill from the next available date
            // If both fails, return false

            TimePoint[] pastData = timeSeries.Where(ts => ts.Date <= weekday).ToArray();
            if (pastData.Length != 0)
            {
                valueTuple = pastData.Last();
                index = timeSeries.FindIndex(ts => ts.Date == pastData.Last().Date) + 1;
                return true;
            }
            
            TimePoint[] futureData = timeSeries.Where(ts => ts.Date >= weekday).ToArray();
            if (futureData.Length != 0)
            {
                valueTuple = futureData.First();
                index = timeSeries.FindIndex(ts => ts.Date == futureData.First().Date);
                return true;
            }
            
            valueTuple = null;
            index = -1;
            return false;
        }
        #endregion
    }
}
