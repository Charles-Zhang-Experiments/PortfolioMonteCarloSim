using Parcel.Shared.DataTypes;
using PortfolioRisk.Core.DataTypes;

namespace PortfolioRisk.Core.DataSourceService
{
    public interface IDataSourceProvider
    {
        /// <summary>
        /// Get historical data of a symbol in a DataGrid object, it contains 5 columns, in this order:
        /// [Date, Open, High, Low, Close, Adj Close, Volumn]
        /// Specific column names may have special symbols attached to it, e.g. "Adj Close*"
        /// </summary>
        public DataGrid GetSymbol(SymbolDefinition symbol);
        
        /// <summary>
        /// Given a symbol, indicate which currency the numbers in its historical data is
        /// </summary>
        public AssetCurrency GetSymbolCurrency(string symbol);
    }
}