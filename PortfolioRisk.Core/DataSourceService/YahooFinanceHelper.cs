using Csv;
using Parcel.Shared.DataTypes;
using System;
using System.Collections.Generic;
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
                
                // Tiemzone info: https://stackoverflow.com/questions/5996320/net-timezoneinfo-from-olson-time-zone
                // { "America/New_York", "Eastern Standard Time" },
                TimeZoneInfo americaNewYorkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                DateTime newYorkTime = TimeZoneInfo.ConvertTimeFromUtc(input.AddHours(-4), americaNewYorkTimeZone);
                string timeStamp = ((DateTimeOffset)newYorkTime).ToUnixTimeSeconds().ToString();
                return timeStamp;   // TODO: NOT WORKING
                /*Test Info: (Conclusion: EST time, only 0:00 at EST)
                 Test date: 2014/01/28-2022/04/07
Toronto (Jerry Sun): https://query1.finance.yahoo.com/v7/finance/download/AAPL?period1=1390885200&period2=1649304000&interval=1mo&events=history&includeAdjustedClose=true
UTC: https://query1.finance.yahoo.com/v7/finance/download/AAPL?period1=1390896000&period2=1649314800&interval=1m&events=history&includeAdjustedClose=true
UTC to Eastern: https://query1.finance.yahoo.com/v7/finance/download/AAPL?period1=1390878000&period2=1649300400&interval=1m&events=history&includeAdjustedClose=true
UTC+1 to Eastern: https://query1.finance.yahoo.com/v7/finance/download/AAPL?period1=1390964400&period2=1649386800&interval=1m&events=history&includeAdjustedClose=true
Actual: https://query1.finance.yahoo.com/v7/finance/download/AAPL?period1=1390953600&period2=1649376000&interval=1mo&events=history&includeAdjustedClose=true
America/New_York*/
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
