using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PortfolioRisk.Core.DataTypes
{
    public enum PnLType
    {
        Asset,
        ExchangeRate
    }
    public class PnL
    {
        public string Asset { get; set; }
        /// <summary>
        /// 5000*260 PnL
        /// </summary>
        public double[][] Values { get; set; }
        public PnLType Type { get; set; }
    }

    public class FXFactor
    {
        /// <summary>
        /// Without exchange rate fluctuation
        /// </summary>
        public double Self { get; set; }
        /// <summary>
        /// Without self-price fluctuation
        /// </summary>
        public double FX { get; set; }
    }
    
    public class Report
    {
        #region Constructor
        public Report(Dictionary<string, double> currentPrices, DateTime priceDate)
        {
            CurrentPrices = currentPrices;
            PriceDate = priceDate;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Number in CAD
        /// </summary>
        public List<PnL> PortfolioReturn { get; set; } = new List<PnL>();
        
        /// <summary>
        /// Tail risk at terminal point at Day 260 with 1% ETL
        /// </summary>
        public Dictionary<string, double>  ETL { get; set; }
        
        /// <summary>
        /// Alternative tail risk
        /// </summary>
        public Dictionary<string, double> MaxETL { get; set; }
        
        /// <summary>
        /// Returns of individual factors
        /// </summary>
        public List<PnL> FactorReturn { get; set; } = new List<PnL>();
        /// <summary>
        /// ETL components for foreign exchange
        /// </summary>
        public Dictionary<string, FXFactor> ETLFxFactors { get; set; }
        /// <summary>
        /// MaxETL components for foreign exchange
        /// </summary>
        public Dictionary<string, FXFactor> MaxETLFxFactors { get; set; }
        
        public Dictionary<string,double> CurrentPrices { get; set; }
        /// <summary>
        /// Original size of investment as allocated by weight
        /// </summary>
        public Dictionary<string, double> InvestmentSize { get; set; }
        /// <summary>
        /// Original size of investment as categorized by currency
        /// </summary>
        public Dictionary<AssetCurrency, double> CurrencySize { get; set; }
        public DateTime PriceDate { get; set; }
        #endregion

        #region Additional Property
        public PortfolioAnalyzer Analyzer { get; set; }
        #endregion

        #region Accessor
        public string BuildSummaryText(bool currentPriceLast = false)
        {
            StringBuilder builder = new StringBuilder();
            if(!currentPriceLast)
                builder.AppendLine(CurrentPrice());
            // ETL
            builder.AppendLine($"ETL:");
            foreach ((string symbol, double etl) in ETL)
            {
                builder.AppendLine($" {symbol,5}:{(long)etl,15:N0} (Risk Exposure: {-(InvestmentSize[symbol] - etl),12:N0})");
                if (ETLFxFactors.ContainsKey(symbol))
                {
                    FXFactor factors = ETLFxFactors[symbol];
                    builder.AppendLine($"     -Self:{(long)factors.Self,15:N0} (Risk Exposure: {-(InvestmentSize[symbol] - factors.Self),12:N0})");
                    builder.AppendLine($"     -FX:  {(long)factors.FX,15:N0} (Risk Exposure: {-(InvestmentSize[symbol] - factors.FX),12:N0})");
                }
            }
            builder.AppendLine($" Total: {ETL.Sum(e => e.Value):N0}");
            // Max ETL
            builder.AppendLine($"Max ETL:");
            foreach ((string symbol, double maxEtl) in MaxETL)
            {
                builder.AppendLine($" {symbol,5}:{(long)maxEtl,15:N0} (Risk Exposure: {-(InvestmentSize[symbol] - maxEtl),12:N0})");
                if (MaxETLFxFactors.ContainsKey(symbol))
                {
                    FXFactor factors = MaxETLFxFactors[symbol];
                    builder.AppendLine($"     -Self:{(long)factors.Self,15:N0} (Risk Exposure: {-(InvestmentSize[symbol] - factors.Self),12:N0})");
                    builder.AppendLine($"     -FX:  {(long)factors.FX,15:N0} (Risk Exposure: {-(InvestmentSize[symbol] - factors.FX),12:N0})");
                }
            }
            builder.AppendLine($" Total: {MaxETL.Sum(e => e.Value):N0}");
            // Current Prices
            if(currentPriceLast)
                builder.AppendLine(CurrentPrice());

            return builder.ToString().TrimEnd();
            
            string CurrentPrice() => $"Current Price ({PriceDate:yyyy-MM-dd}): {string.Join(", ", CurrentPrices.Select(cp => $"{cp.Key}: {cp.Value:N2}"))}";
        }
        #endregion
    }
}