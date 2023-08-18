using System;

namespace PortfolioRisk.Core.DataTypes
{
    public readonly struct TimeSeries
    {
        public DateTime Date { get; }
        public double Value { get; }

        public TimeSeries(DateTime date, double value)
        {
            Date = date;
            Value = value;
        }
    }
}