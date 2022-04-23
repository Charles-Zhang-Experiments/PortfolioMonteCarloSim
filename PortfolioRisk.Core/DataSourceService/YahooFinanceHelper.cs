using Csv;
using Parcel.Shared.DataTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace PortfolioRisk.Core.DataSourceService
{
    public enum YahooTimeInterval
    {
        Month,
        Day,
        Week,
        Year
    }
    public class YahooFinanceParameter
    {
        public string InputSymbol { get; set; }
        public DateTime InputStartDate { get; set; }
        public DateTime InputEndDate { get; set; }
        public YahooTimeInterval InputInterval { get; set; }
        public DataGrid OutputTable { get; set; }
    }

    /// <summary>
    /// Fetches time series data from yahoo finance using non-developer web-api
    /// </summary>
    public static class YahooFinanceHelper
    {
        public static Dictionary<string, string> Remapping { get; set; } = new Dictionary<string, string>()
        {
            { "XIU", "XIU.TO" },
        };

        public static void GetHistorical(YahooFinanceParameter parameter)
        {
            string ConvertTimeFormat(DateTime input)
            {
                input = input.Date; // Clear out time, set to 0
                string timeStamp = (input - new DateTime(1970, 01, 01)).TotalSeconds.ToString(CultureInfo.InvariantCulture);
                return timeStamp;
            }

            string RemapSymbols(string original)
                => Remapping.ContainsKey(original) ? Remapping[original] : original;

            Dictionary<YahooTimeInterval, string> validIntervals = new Dictionary<YahooTimeInterval, string>()
            {
                {YahooTimeInterval.Month, "1mo"},
                {YahooTimeInterval.Day, "1d"},
                {YahooTimeInterval.Week, "1w"},
                {YahooTimeInterval.Year, "1y"},
            };
            if (parameter.InputStartDate > parameter.InputEndDate)
                throw new ArgumentException("Wrong date.");
            if (parameter.InputEndDate > DateTime.Now)
                throw new ArgumentException("Wrong date.");
            if (parameter.InputSymbol.Length > 7)
                throw new ArgumentException("Wrong symbol.");

            string startTime = ConvertTimeFormat(parameter.InputStartDate);
            string endTime = ConvertTimeFormat(parameter.InputEndDate);
            string interval = validIntervals[parameter.InputInterval];
            string csvUrl =
                $"https://query1.finance.yahoo.com/v7/finance/download/{RemapSymbols(parameter.InputSymbol)}?period1={startTime}&period2={endTime}&interval={interval}&events=history&includeAdjustedClose=true";
            string csvText = new WebClient().DownloadString(csvUrl);
            IEnumerable<ICsvLine> csv = Csv.CsvReader.ReadFromText(csvText, new CsvOptions()
            {
                HeaderMode = HeaderMode.HeaderPresent
            });
            parameter.OutputTable = new DataGrid(csv);
        }
    }
}
