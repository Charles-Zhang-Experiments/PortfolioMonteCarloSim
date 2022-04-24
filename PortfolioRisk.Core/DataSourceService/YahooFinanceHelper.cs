using Csv;
using Parcel.Shared.DataTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using PortfolioRisk.Core.DataTypes;

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
    public class YahooFinanceHelper: IDataSourceProvider
    {
        #region Data
        private Dictionary<string, string> Remapping { get; set; } = new Dictionary<string, string>()
        {
            // Tickers
            { "XIU", "XIU.TO" },
            // Exchange rates
            { "USD/CAD", "CAD=X" },
        };
        #endregion
        
        #region Public Interface
        public DataGrid GetSymbol(SymbolDefinition symbol)
        {
            var parameters = new YahooFinanceParameter()
            {
                InputInterval = YahooTimeInterval.Day,
                InputSymbol = symbol.Name,
                InputStartDate = symbol.QueryStartDate,
                InputEndDate = symbol.QueryEndDate
            };
            GetHistorical(parameters);
            return parameters.OutputTable;
        }
        public AssetCurrency GetSymbolCurrency(AnalysisConfig config, string symbol)
        {
            if (config.CurrencyMapping.ContainsKey(symbol))
                return config.CurrencyMapping[symbol];
            throw new ArgumentException("Undefined symbol.");
        }
        #endregion

        #region Helpers
        private void GetHistorical(YahooFinanceParameter parameter)
        {
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
            if (parameter.InputEndDate > DateTime.Now.AddDays(1))
                throw new ArgumentException("Wrong date.");
            if (parameter.InputSymbol.Length > 7)
                throw new ArgumentException("Wrong symbol.");

            string startTime = ConvertTimeFormat(parameter.InputStartDate);
            string endTime = ConvertTimeFormat(parameter.InputEndDate);
            string interval = validIntervals[parameter.InputInterval];
            string csvUrl =
                $"https://query1.finance.yahoo.com/v7/finance/download/{RemapSymbols(parameter.InputSymbol)}?period1={startTime}&period2={endTime}&interval={interval}&events=history&includeAdjustedClose=true";
            string csvText = FetchUrlText(csvUrl);
            IEnumerable<ICsvLine> csv = CsvReader.ReadFromText(csvText, new CsvOptions()
            {
                HeaderMode = HeaderMode.HeaderPresent
            });
            parameter.OutputTable = new DataGrid(csv);
        }
        public static string ConvertTimeFormat(DateTime input)
        {
            input = input.Date; // Clear out time, set to 0
            string timeStamp = (input - new DateTime(1970, 01, 01)).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            return timeStamp;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Equivalent to `new WebClient().DownloadString(url)` but more generally available, e.g. on web platform
        /// </summary>
        private string FetchUrlText(string url)
        {
            HttpClient client = new HttpClient();
            using HttpResponseMessage response = client.GetAsync(url).Result;
            using HttpContent content = response.Content;
            return content.ReadAsStringAsync().Result;
        }
        #endregion
    }
}
