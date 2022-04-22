using Csv;
using Parcel.Shared.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PortfolioRisk.Core.DataSourceService
{
    public static class OfflineSourceHelper
    {
        public static string[] OfflineSources = new string[] { "SPY", "XIU", "USD/CAD" };
        public static Dictionary<string, string> Remapping { get; set; } = new Dictionary<string, string>()
        {
            { "USD/CAD", "USD=CAD" },
        };

        public static DataGrid GetSymbol(string symbol)
        {
            string RemapSymbols(string original)
                => Remapping.ContainsKey(original) ? Remapping[original] : original;

            symbol = RemapSymbols(symbol);
            if (OfflineSources.Contains(symbol))
            {
                string csvText = ReadTextResource($"PortfolioRisk.Core.OfflineSources.{symbol}.csv");
                IEnumerable<ICsvLine> csv = Csv.CsvReader.ReadFromText(csvText, new CsvOptions()
                {
                    HeaderMode = HeaderMode.HeaderPresent
                });
                return new DataGrid(csv);
            }

            return null;
        }

        public static string ReadTextResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetCallingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(Parcel)))
            {
                resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
