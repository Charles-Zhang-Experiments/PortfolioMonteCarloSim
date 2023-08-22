using Dapper;
using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace PortfolioRisk.Core
{
    public class ExamplesHelper
    {
        #region Construction
        public string DatabasePath { get; }
        public ExamplesHelper(string databasePath)
        {
            if (!File.Exists(databasePath))
                throw new ArgumentException($"File {databasePath} doesn't exist.");
            DatabasePath = databasePath;
        }
        #endregion

        #region Methods
        public TimeSeries GetSymbol(SymbolDefinition symbol, string attribute = null)
        {
            return new TimeSeries()
            {
                Name = symbol.TickerName,
                DataPoints = QuerySymbolAttribute(symbol.TickerName, attribute),
                Symbol = symbol,
            };
        }
        public string GetSymbolCurrency(string symbol)
        {
            if (symbol.Contains('/'))
                return symbol.Split('/').Last();

            var queryResult = QuerySymbolCurrency(symbol);
            if (queryResult == null)
                throw new ArgumentException($"Cannot find currency for symbol: {symbol}");
            return queryResult;
        }
        #endregion

        #region Query Routines
        private TimePoint[] QuerySymbolAttribute(string symbol, string attribute)
        {
            using SQLiteConnection connection = new($"Data Source={DatabasePath}");
            connection.Open();

            return connection.Query<TimePoint>($"""
                SELECT 
                	Date,
                	{attribute} as Value
                FROM Symbols
                WHERE Symbol = '{symbol}'
                """).ToArray();
        }
        private string QuerySymbolCurrency(string symbol)
        {
            using SQLiteConnection connection = new($"Data Source={DatabasePath}");
            connection.Open();

            return connection.Query<string>($"""
                SELECT Currency
                FROM SymbolCurrency
                WHERE Symbol = '{symbol}'
                """).SingleOrDefault();
        }
        #endregion
    }
}
