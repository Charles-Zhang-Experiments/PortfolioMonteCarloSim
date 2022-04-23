using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PortfolioRisk.Core.DataTypes;

namespace PortfolioRisk.Core.Algorithm
{
    public class HistoricalSimulation
    {
        #region Constructor
        public HistoricalSimulation(Dictionary<string,List<TimeSeries>> rawData)
        {
            RawData = rawData;
            ReturnData = ComputeReturn(rawData);
            
            // Some basic data validation
            MinDate = RawData.Values.First().First().Date;
            MaxDate = RawData.Values.First().Last().Date;
            if (MaxDate < MinDate)
                throw new InvalidDataException("Wrong date range for time series.");
            if (RawData.Any(d => d.Value.First().Date != MinDate || d.Value.Last().Date != MaxDate))
                throw new InvalidDataException("Mismatching range for time series.");
        }
        #endregion

        #region Constants
        public const int QuarterDays = 66;
        public const int QuarterReturnDays = 65;
        public const int YearReturnDays = QuarterReturnDays * 4; 
        #endregion

        #region Private Members
        private readonly Random _randomGenerator = new Random();
        private DateTime MaxDate { get; }
        private DateTime MinDate { get; }
        private Dictionary<string,List<TimeSeries>> RawData { get; }
        private Dictionary<string,List<TimeSeries>> ReturnData { get; }
        #endregion

        #region Interface Method
        public Dictionary<string, double[]> SimulateOnce(Dictionary<string, double> currentPrices)
        {
            // Randomly pick 4 quarters of historical data
            DateTime[] startDates = Enumerable.Range(0, 4)
                .Select(_ => PickRandomDate(MinDate, MaxDate.AddDays(-QuarterDays)))
                .ToArray();
            // Select return time series and stitch
            Dictionary<string, TimeSeries[]> stitchReturns = 
                ReturnData.ToDictionary(rd => rd.Key, rd => 
                    startDates
                        .SelectMany(
                            sd => 
                                rd.Value
                                    .Skip(rd.Value.FindIndex(ts => ts.Date == sd))
                                    .Take(QuarterReturnDays))
                        .ToArray());
            
            // Validation Assertion
            if (stitchReturns.Any(sr => sr.Value.Length != YearReturnDays))
                throw new InvalidOperationException("Unexpected stitching result.");

            // Simulate price path
            Dictionary<string, double[]> simulatedPaths = stitchReturns.ToDictionary(sr => sr.Key, sr =>
            {
                double currentPrice = currentPrices[sr.Key];
                List<double> path = new List<double>() { currentPrice };
                sr.Value.Aggregate(currentPrice,
                    (agg, ts) =>
                    {
                        var accu = agg * ts.Value;
                        path.Add(accu);
                        return accu;
                    });
                return path.ToArray();
            });

            return simulatedPaths;
        }
        #endregion

        #region Helpers
        private DateTime PickRandomDate(DateTime start, DateTime end)
            => start.AddDays(_randomGenerator.Next((end - start).Days));
        #endregion

        #region Routines
        private static Dictionary<string, List<TimeSeries>> ComputeReturn(Dictionary<string, List<TimeSeries>> rawData)
        {
            Dictionary<string, List<TimeSeries>> result = new Dictionary<string, List<TimeSeries>>();

            foreach (KeyValuePair<string,List<TimeSeries>> timeSeries in rawData)
            {
                string ticker = timeSeries.Key;
                List<TimeSeries> valueSeries = timeSeries.Value;
                List<TimeSeries> newSeries = new List<TimeSeries>();
                for (int i = valueSeries.Count - 1; i > 0; i--)
                {
                    DateTime date = valueSeries[i].Date;
                    double value = (valueSeries[i].Value - valueSeries[i - 1].Value) / valueSeries[i - 1].Value;
                    newSeries.Add(new TimeSeries(date, value));
                }
                
                result.Add(ticker, newSeries);
            }

            return result;
        }
        #endregion
    }
}