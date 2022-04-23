using Parcel.Shared.DataTypes;
using PortfolioRisk.Core.DataSourceService;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioRisk.Core
{
    public class PortfolioAnalyzer
    {
        #region Internal Data
        private Dictionary<string, DataGrid> TimeSeries { get; set; } = new Dictionary<string, DataGrid>();
        #endregion

        public void Run(AnalysisConfig config)
        {
            // Fetch time series data
            foreach (string symbol in config.Assets.Union(config.Factors))
            {
                if (TimeSeries.ContainsKey(symbol)) continue;

                TimeSeries[symbol] = PreprocessAndFetchSymbol(symbol, config);
            }

            // Pre-process and clean data (matching date, matching number of row, and fill in missing data)
            foreach (KeyValuePair<string, DataGrid> pair in TimeSeries)
            {

                Console.WriteLine(); // Report missing entries
            }
        }

        #region Routines
        /// <summary>
        /// Automatically handle conversion of common names and interest rate
        /// </summary>
        private DataGrid PreprocessAndFetchSymbol(string originalSymbol, AnalysisConfig config)
        {
            // Offline sources
            if (OfflineSourceHelper.OfflineSources.Contains(originalSymbol))
            {
                return OfflineSourceHelper.GetSymbol(originalSymbol);
            }
            // Interest rates
            else if (originalSymbol.Contains('/'))
            {
                throw new NotImplementedException();
            }
            else
            {
                string symbol = originalSymbol;
                YahooFinanceParameter parameter = new YahooFinanceParameter()
                {
                    InputSymbol = symbol,
                    InputInterval = YahooTimeInterval.Day,
                    InputStartDate = new DateTime(2016, 12, 31),
                    InputEndDate = new DateTime(2021, 12, 31),
                };
                YahooFinanceHelper.GetHistorical(parameter);
                return parameter.OutputTable;
            }
        }
        #endregion
    }
}
