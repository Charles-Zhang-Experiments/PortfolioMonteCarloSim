using Parcel.Shared.DataTypes;
using PortfolioRisk.Core.DataSourceService;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioRisk.Core
{
    public class PortfolioAnalyzer
    {
        #region Internal Data
        private Dictionary<string, DataGrid> TimeSeries { get; set; } = new Dictionary<string, DataGrid>();
        #endregion

        public void Run(AnalysisConfig config)
        {
            // Fetch time series data
            foreach (string symbol in config.Assets.Union(config.Factors))
            {
                if (TimeSeries.ContainsKey(symbol)) continue;

                // Sort from past to present
                DataGrid table = PreprocessAndFetchSymbol(symbol, config);
                table.Sort(table.Columns.First().Header, false);
                TimeSeries[symbol] = table;
            }

            // Yahoo Finance returns data with incomplete entries and sometimes wrong range of date;
            // So we need to pre-process and clean data
            // (unify dates, and fill in missing data for all weekdays, then we get matching number of rows)
            
            // Intersect dates and find minimally shared range of date sequence
            IEnumerable<DateTime>[] datesSeries = TimeSeries.Values.Select(v => v.Columns.First().GetDataAs<DateTime>()).ToArray();
            DateTime[] intersection = datesSeries.Skip(1)
                .Aggregate(new HashSet<DateTime>(datesSeries.First()), (h, e) =>
                {
                    h.IntersectWith(e);
                    return h;
                }).OrderBy(dt => dt).ToArray();
            DateTime[] weekDays = GetWorkDays(intersection.Min(), intersection.Max());
            // Fill in missing data for all weekdays
            foreach (KeyValuePair<string, DataGrid> pair in TimeSeries)
            {
                // Extract time series
                string ticker = pair.Key;
                DataGrid table = pair.Value;
                var dateColumn = table.Columns.First().GetDataAs<DateTime>();
                var adjustedCloseColumn = table.Columns[4].GetDataAs<double>();
                List<(DateTime First, double Second)> timeSeries = dateColumn.Zip(adjustedCloseColumn).ToList();

                for (int i = 0; i < weekDays.Length; i++)
                {
                    DateTime weekday = weekDays[i];
                    if (timeSeries.Any(ts => ts.First == weekday))
                        continue;
                    
                    if (FindSuitableFillinDate(timeSeries, weekday, weekDays,
                        out (DateTime First, double Second) substitute, out int index))
                        timeSeries.Insert(index, (First: weekday, Second: substitute.Second));
                    else throw new InvalidOperationException($"Cannot back-fill time series for {ticker}!");
                }
                Console.WriteLine(); // Report missing entries
            }
        }

        #region Routines
        /// <summary>
        /// Automatically handle conversion of common names and interest rate
        /// </summary>
        private DataGrid PreprocessAndFetchSymbol(string originalSymbol, AnalysisConfig config)
        {
            // Offline sources
            if (OfflineSourceHelper.OfflineSources.Contains(originalSymbol))
            {
                return OfflineSourceHelper.GetSymbol(originalSymbol);
            }
            // Interest rates
            else if (originalSymbol.Contains('/'))
            {
                throw new NotImplementedException();
            }
            else
            {
                string symbol = originalSymbol;
                YahooFinanceParameter parameter = new YahooFinanceParameter()
                {
                    InputSymbol = symbol,
                    InputInterval = YahooTimeInterval.Day,
                    InputStartDate = new DateTime(2016, 12, 31),
                    InputEndDate = new DateTime(2021, 12, 31),
                };
                YahooFinanceHelper.GetHistorical(parameter);
                return parameter.OutputTable;
            }
        }

        /// <summary>
        /// Get a sequence of all workdays between two end points
        /// </summary>
        private DateTime[] GetWorkDays(DateTime start, DateTime end)
        {
            return Enumerable.Range(0, (end - start).Days)
                .Select(i => start.AddDays(i))
                .Where(dt => dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday)
                .ToArray();
        }
        
        /// <summary>
        /// Given a target date (that's absent in a given time series) and a reference,
        /// find the most suitable entry to back-fill data for that date
        /// </summary>
        private bool FindSuitableFillinDate(List<(DateTime First, double Second)> timeSeries, DateTime weekday, DateTime[] weekDays, out (DateTime First, double Second) valueTuple, out int i)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
