using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PortfolioRisk.Core.DataTypes
{
    public class PnL
    {
        public string Asset { get; set; }
        /// <summary>
        /// 5000*260 PnL
        /// </summary>
        public double[][] Values { get; set; }
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
        
        public Dictionary<string,double> CurrentPrices { get; set; }
        /// <summary>
        /// Original size of investment as allocated by weight
        /// </summary>
        public Dictionary<string, double> InvestmentSize { get; set; }
        public DateTime PriceDate { get; set; }
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
                builder.AppendLine($" {symbol,5}:{(long)etl,15:N0} (Risk Exposure: {-(InvestmentSize[symbol] - etl),12:N0})");
            builder.AppendLine($" Total: {ETL.Sum(e => e.Value):N0}");
            // Max ETL
            builder.AppendLine($"Max ETL:");
            foreach ((string symbol, double maxEtl) in MaxETL)
                builder.AppendLine($" {symbol,5}:{(long)maxEtl,15:N0} (Risk Exposure: {-(InvestmentSize[symbol] - maxEtl),12:N0})");
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