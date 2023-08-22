using System;

namespace PortfolioRisk.Core
{
    public record SymbolDefinition(string TickerName, DateTime TimeSeriesStartDate, DateTime TimeSeriesEndDate, string Currency);
    public sealed class TimeSeries
    {
        public string Name;
        public TimePoint[] DataPoints;
        public SymbolDefinition Symbol;
    }
    public readonly struct TimePoint
    {
        public DateTime Date { get; }
        public double Value { get; }

        public TimePoint(DateTime date, double value)
        {
            Date = date;
            Value = value;
        }
    }
}
