using System.Collections.Generic;

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
    }
}