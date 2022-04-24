using PortfolioRisk.Core.DataTypes;

namespace PortfolioBuilder.Models
{
    public class AssetDefinition
    {
        public string Symbol { get; set; }
        public AssetCurrency Currency { get; set; }
        public double Weight { get; set; }

        #region Constructors
        public AssetDefinition(){}
        public AssetDefinition(string symbol, AssetCurrency currency, double weight)
        {
            Symbol = symbol;
            Currency = currency;
            Weight = weight;
        }
        #endregion
    }
}