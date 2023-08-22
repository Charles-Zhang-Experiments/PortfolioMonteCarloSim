using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PortfolioRisk.Core
{
    public class BatchHistoricalSimulation
    {
        #region Data Reference
        private Random _randomGenerator { get; }
        private DateTime MaxRangeUsableDate { get; }
        private DateTime MaxDate { get; }
        private DateTime MinDate { get; }
        private Dictionary<string, TimePoint[]> ReturnData { get; }
        #endregion

        #region Constructor
        public BatchHistoricalSimulation(Dictionary<string, List<TimePoint>> rawData, int windowReturnDays = 65, Random generator = null)
        {
            ReturnData = ComputeReturns(rawData);

            // Some basic data validation
            MinDate = ReturnData.Values.First().First().Date;
            MaxDate = ReturnData.Values.First().Last().Date;
            if (MaxDate < MinDate)
                throw new InvalidDataException("Wrong date range for time series.");
            if (ReturnData.Any(d => d.Value.First().Date != MinDate || d.Value.Last().Date != MaxDate))
                throw new InvalidDataException("Mismatching range for time series.");
            if (rawData.Values.Select(v => v.Count).Distinct().Count() != 1)
                throw new InvalidDataException("Mismatching size for time series.");

            WindowReturnDays = windowReturnDays;
            _randomGenerator = generator == null ? new Random() : generator;

            TimePoint[] sample = ReturnData.Values.First();
            // Pick it so that when we take a window-size away from this date thus we have sufficient amount of data
            MaxRangeUsableDate = sample[Array.FindIndex(sample, d => d.Date == MaxDate) - WindowReturnDays].Date;
        }
        #endregion

        #region Constants
        public int WindowReturnDays { get; }
        public int WindowDays => WindowReturnDays + 1; // Remark-cz: e.g. for a quarter return of 65 days, the actual window would be 66 days; For a sample of 4 quarters (i.e. for a year), that would be picking 65*4 days
        #endregion

        #region Interface Method
        /// <summary>
        /// Simulate and generate returns for all assets in one scenario;
        /// The outcome numbers at each array element is the total return from the start date at index 0
        /// </summary>
        public Dictionary<string, double[]> SimulateOnce(int windows = 4)
        {
            // Randomly pick N windows of historical data
            // Remark-cz: Assume all time series (returns) all have the exact the same date stamps
            DateTime[] startDates = PickWindowStartDates(windows, WindowReturnDays, _randomGenerator, MinDate, MaxRangeUsableDate);

            // Select return series and stitch
            Dictionary<string, TimePoint[]> stitchReturns = StitchReturns(startDates, windows, ReturnData);

            // Validation Assert
            if (stitchReturns.Any(sr => sr.Value.Length != WindowReturnDays * windows))
                throw new InvalidOperationException("Unexpected stitching result.");

            // Simulate price (accumulated return) path
            var simulationPaths = SimulatePaths(stitchReturns);

            // Final validation
            if (simulationPaths.Values.Any(p => p.Length != WindowReturnDays * windows))
                throw new InvalidOperationException("Unexpected path length.");

            return simulationPaths;
        }
        #endregion

        #region Routines
        private static Dictionary<string, double[]> SimulatePaths(Dictionary<string, TimePoint[]> stitchReturns)
        {
            return stitchReturns.ToDictionary(sr => sr.Key, sr =>
            {
                List<double> path = new() { sr.Value.First().Value };
                sr.Value.Skip(1).Aggregate(sr.Value.First().Value,
                    (agg, ts) =>
                    {
                        double accu = agg * ts.Value; // Apply all returns until a given day
                        path.Add(accu);
                        return accu;
                    });
                return path.ToArray();
            });
        }
        private static Dictionary<string, TimePoint[]> StitchReturns(DateTime[] startDates, int windowSize, Dictionary<string, TimePoint[]> returnData)
        {
            return
                returnData.ToDictionary(rd => rd.Key, rd =>
                    startDates
                        .SelectMany(
                            sd =>
                                rd.Value
                                    .Skip(Array.FindIndex(rd.Value, ts => ts.Date == sd))
                                    .Take(windowSize))
                        .ToArray());
        }
        private static DateTime[] PickWindowStartDates(int windows, int windowSize, Random random, DateTime rangeStartDate, DateTime rangeEndDate)
        {
            return Enumerable.Range(0, windows)
                .Select(_ => PickRandomDate(rangeStartDate, rangeEndDate))
                .ToArray();

            DateTime PickRandomDate(DateTime start, DateTime end)
                => start.AddDays(random.Next((end - start).Days));
        }
        private static Dictionary<string, TimePoint[]> ComputeReturns(Dictionary<string, List<TimePoint>> rawData)
        {
            Dictionary<string, TimePoint[]> result = new();

            foreach ((string ticker, List<TimePoint> oldSeries) in rawData)
            {
                var orderedSeries = oldSeries.OrderBy(o => o.Date).ToArray();
                List<TimePoint> newSeries = new();
                for (int i = orderedSeries.Length - 1; i > 0; i--)
                {
                    DateTime date = orderedSeries[i].Date;
                    double value = orderedSeries[i].Value / orderedSeries[i - 1].Value;
                    newSeries.Add(new TimePoint(date, value));
                }

                result.Add(ticker, newSeries.OrderBy(ns => ns.Date).ToArray());
            }

            return result;
        }
        #endregion
    }
}