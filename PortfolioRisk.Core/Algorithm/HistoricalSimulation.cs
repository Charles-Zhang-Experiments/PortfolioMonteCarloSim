﻿using System;
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
            ReturnData = ComputeReturn(rawData);
            
            // Some basic data validation
            MinDate = ReturnData.Values.First().First().Date;
            MaxDate = ReturnData.Values.First().Last().Date;
            if (MaxDate < MinDate)
                throw new InvalidDataException("Wrong date range for time series.");
            if (ReturnData.Any(d => d.Value.First().Date != MinDate || d.Value.Last().Date != MaxDate))
                throw new InvalidDataException("Mismatching range for time series.");
        }
        #endregion

        #region Constants
        // public const int QuarterDays = 66;
        public const int QuarterReturnDays = 65;
        public const int YearReturnDays = QuarterReturnDays * 4;
        #endregion

        #region Private Members
        private readonly Random _randomGenerator = new Random();
        private DateTime MaxDate { get; }
        private DateTime MinDate { get; }
        private Dictionary<string, TimeSeries[]> ReturnData { get; }
        #endregion

        #region Interface Method
        /// <summary>
        /// Simulate and generate returns for all assets in one scenario;
        /// The outcome numbers at each array element is the total return from the start date at index 0
        /// </summary>
        public Dictionary<string, double[]> SimulateOnce()
        {
            #region Simulation Logic
            // Randomly pick 4 quarters of historical data
            DateTime[] startDates = PickQuarterStartDates();
            
            // Select return series and stitch
            Dictionary<string, TimeSeries[]> stitchReturns = StitchReturns();
            
            // Validation Assert
            if (stitchReturns.Any(sr => sr.Value.Length != YearReturnDays))
                throw new InvalidOperationException("Unexpected stitching result.");

            // Simulate price (accumulated return) path
            return SimulatePaths();
            #endregion

            #region Local Functions
            DateTime[] PickQuarterStartDates()
            {
                DateTime rangeStartDate = MinDate;
                DateTime rangeEndDate = ReturnData.First().Value
                        [Array.FindIndex(ReturnData.First().Value, d => d.Date == MaxDate) - QuarterReturnDays]
                    .Date; // Pick it so that when we take a quarter from this date, we have sufficient amount of data
                return startDates = Enumerable.Range(0, 4)
                    .Select(_ => PickRandomDate(rangeStartDate, rangeEndDate))
                    .ToArray();
            }

            Dictionary<string, TimeSeries[]> StitchReturns()
            {
                return
                    ReturnData.ToDictionary(rd => rd.Key, rd =>
                        startDates
                            .SelectMany(
                                sd =>
                                    rd.Value
                                        .Skip(Array.FindIndex(rd.Value, ts => ts.Date == sd))
                                        .Take(QuarterReturnDays))
                            .ToArray());
            }

            Dictionary<string, double[]> SimulatePaths()
            {
                return stitchReturns.ToDictionary(sr => sr.Key, sr =>
                {
                    List<double> path = new List<double>() {sr.Value.First().Value};
                    sr.Value.Skip(1).Aggregate(sr.Value.First().Value,
                        (agg, ts) =>
                        {
                            double accu = agg * ts.Value; // Apply all returns until a given day
                            path.Add(accu);
                            return accu;
                        });

                    if (path.Count != YearReturnDays)
                        throw new InvalidOperationException("Unexpected path length.");
                    return path.ToArray();
                });
            }
            #endregion
        }
        #endregion

        #region Helpers
        private DateTime PickRandomDate(DateTime start, DateTime end)
            => start.AddDays(_randomGenerator.Next((end - start).Days));
        #endregion

        #region Routines
        private static Dictionary<string, TimeSeries[]> ComputeReturn(Dictionary<string, List<TimeSeries>> rawData)
        {
            Dictionary<string, TimeSeries[]> result = new Dictionary<string, TimeSeries[]>();

            foreach ((string ticker, List<TimeSeries> oldSeries) in rawData)
            {
                List<TimeSeries> newSeries = new List<TimeSeries>();
                for (int i = oldSeries.Count - 1; i > 0; i--)
                {
                    DateTime date = oldSeries[i].Date;
                    double value = oldSeries[i].Value / oldSeries[i - 1].Value;
                    newSeries.Add(new TimeSeries(date, value));
                }
                
                result.Add(ticker, newSeries.OrderBy(ns => ns.Date).ToArray());
            }

            return result;
        }
        #endregion
    }
}