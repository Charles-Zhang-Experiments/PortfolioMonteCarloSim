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
        }
        #endregion

        #region Constants
        private const int QuarterDays = 66;
        private const int QuarterReturnDays = 65;
        #endregion

        #region Private Members
        private readonly Random _randomGenerator = new Random();
        private Dictionary<string,List<TimeSeries>> RawData { get; }
        private Dictionary<string,List<TimeSeries>> ReturnData { get; }
        #endregion

        #region Interface Method
        public void Simulate(AnalysisConfig config)
        {
            // Some basic data validation
            DateTime min = RawData.Values.First().First().Date;
            DateTime max = RawData.Values.First().Last().Date;
            if (max < min)
                throw new InvalidDataException("Wrong date range for time series.");
            if (RawData.Any(d => d.Value.First().Date != min || d.Value.Last().Date != max))
                throw new InvalidDataException("Mismatching range for time series.");
            
            // Randomly pick 4 quarters of historical data
            DateTime[][] startDates = Enumerable.Range(0, 4)
                .Select(_ => PickRandomDate(min, max.AddDays(-QuarterDays)))
                .Select(randomStart => PortfolioAnalyzer.GetWorkDays(randomStart, randomStart.AddDays(QuarterDays)))
                .ToArray();
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
                    resultColumn.Add((sourceColumn[i] - sourceColumn[i-1]) / sourceColumn[i-1]);
                }
                
                result.Add(ticker, newSeries);
            }

            return result;
        }
        #endregion
    }
}