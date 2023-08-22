using Csv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Linq;
using System.IO;

namespace PortfolioRisk.Core
{
    /// <summary>
    /// Fetches time series data from yahoo finance using non-developer web-api
    /// </summary>
    public class YahooFinanceHelper
    {
        #region Types
        public enum YahooTimeInterval
        {
            OneMinute,
            TwoMinute,
            FiveMinute,
            FifteenMinute,
            ThirtyMinute,
            SixtyMinute,
            NinetyMinute,
            OneHour,
            OneDay,
            FiveDay,
            OneWeek,
            OneMonth,
            ThreeMonths
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// Get historical data of a symbol in a DataGrid object, it contains 5 columns, in this order: 
        /// [Date, Open, High, Low, Close, Adj Close, Volumn]
        /// Some tickers may have special column names e.g. "Adj Close*"
        /// </summary>
        public TimeSeries GetSymbol(SymbolDefinition symbol, YahooTimeInterval interval, string attribute = null, string saveCopyLocation = null)
        {
            Dictionary<YahooTimeInterval, string> validIntervals = new Dictionary<YahooTimeInterval, string>()
            {
                {YahooTimeInterval.OneMinute, "1m"},
                {YahooTimeInterval.TwoMinute, "2m"},
                {YahooTimeInterval.FiveMinute, "5m"},
                {YahooTimeInterval.FifteenMinute, "15m"},
                {YahooTimeInterval.ThirtyMinute, "30m"},
                {YahooTimeInterval.SixtyMinute, "60m"},
                {YahooTimeInterval.NinetyMinute, "90m"},
                {YahooTimeInterval.OneHour, "1h"},
                {YahooTimeInterval.OneDay, "1d"},
                {YahooTimeInterval.FiveDay, "5d"},
                {YahooTimeInterval.OneWeek, "1wk"},
                {YahooTimeInterval.OneMonth, "1mo"},
                {YahooTimeInterval.ThreeMonths, "3mo"},
            };
            if (symbol.TimeSeriesStartDate > symbol.TimeSeriesEndDate)
                throw new ArgumentException("Wrong date.");
            if (symbol.TimeSeriesEndDate > DateTime.Now.AddDays(1))
                throw new ArgumentException("Wrong date.");
            if (symbol.TickerName.Length > 7)
                throw new ArgumentException("Invalid symbol name.");

            string startTime = ConvertTimeFormat(symbol.TimeSeriesStartDate);
            string endTime = ConvertTimeFormat(symbol.TimeSeriesEndDate);
            string intervalString = validIntervals[interval];
            string csvUrl =
                $"https://query1.finance.yahoo.com/v7/finance/download/{symbol.TickerName}?period1={startTime}&period2={endTime}&interval={intervalString}&events=history&includeAdjustedClose=true";
            string csvText = FetchUrlText(csvUrl);

            // Save to output location
            if (saveCopyLocation != null)
                File.WriteAllText(saveCopyLocation, csvText);

            // Output time series object
            IEnumerable<ICsvLine> csv = CsvReader.ReadFromText(csvText, new CsvOptions()
            {
                HeaderMode = HeaderMode.HeaderPresent,
                Aliases = new List<string[]>
                {
                    new string[] { "Adj Close", "Adj Close*" }
                }
            });
            return new TimeSeries()
            {
                Name = symbol.TickerName,
                DataPoints = csv.Select(line => new TimePoint(DateTime.Parse(line["Date"]), double.Parse(line[attribute ?? "Adj Close"]))).ToArray(), // Get specific attribute
                Symbol = symbol,
            };
        }
        public string GetSymbolCurrency(string symbol)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Helpers
        private static string ConvertTimeFormat(DateTime input)
        {
            input = input.Date; // Clear out time, set to 0
            string timeStamp = (input - new DateTime(1970, 01, 01)).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            return timeStamp;
        }
        /// <summary>
        /// Equivalent to `new WebClient().DownloadString(url)` but more generally available, e.g. on web platform
        /// </summary>
        private static string FetchUrlText(string url)
        {
            HttpClient client = new HttpClient();
            using HttpResponseMessage response = client.GetAsync(url).Result;
            using HttpContent content = response.Content;
            return content.ReadAsStringAsync().Result;
        }
        #endregion
    }
}
