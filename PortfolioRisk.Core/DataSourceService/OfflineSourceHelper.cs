using Csv;
using Parcel.Shared.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PortfolioRisk.Core.DataTypes;

namespace PortfolioRisk.Core.DataSourceService
{
    public class OfflineSourceHelper: IDataSourceProvider
    {
        #region Data
        public static readonly string[] OfflineSources = new string[] { "SPY", "XIU", "USD/CAD" };
        private Dictionary<string, string> FilenameRemapping { get; } = new Dictionary<string, string>()
        {
            { "USD/CAD", "USD=CAD" },
        };
        private Dictionary<string, AssetCurrency> CurrencyMapping { get; } = new Dictionary<string, AssetCurrency>()
        {
            {"SPY", AssetCurrency.USD},
            {"XIU", AssetCurrency.CAD},
            {"USD/CAD", AssetCurrency.USD_TO_CAD},
        };
        #endregion

        #region Interface Methods
        public DataGrid GetSymbol(SymbolDefinition symbol)
        {
            if (OfflineSources.Contains(symbol.Name))
            {
                string csvText = ReadTextResource($"PortfolioRisk.Core.OfflineSources.{RemapSymbols(symbol.Name)}.csv");
                IEnumerable<ICsvLine> csv = CsvReader.ReadFromText(csvText, new CsvOptions()
                {
                    HeaderMode = HeaderMode.HeaderPresent
                });
                return new DataGrid(csv);
            }

            return null;
        }
        public AssetCurrency GetSymbolCurrency(AnalysisConfig config, string symbol)
        {
            if (CurrencyMapping.TryGetValue(symbol, out AssetCurrency value))
                return value;
            else if (config.CurrencyMapping.TryGetValue(symbol, out value))
                return value;
            throw new ArgumentException("Undefined symbol.");
        }
        #endregion

        #region Helpers
        private string ReadTextResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetCallingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(PortfolioRisk)))
            {
                resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));
            }

            using Stream stream = assembly.GetManifestResourceStream(resourcePath);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
        private string RemapSymbols(string original)
            => FilenameRemapping.TryGetValue(original, out string value) ? value : original;
        #endregion
    }
}
