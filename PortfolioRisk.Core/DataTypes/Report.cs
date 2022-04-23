namespace PortfolioRisk.Core.DataTypes
{
    public struct Report
    {
        /// <summary>
        /// Number in CAD or % return
        /// </summary>
        public double PortfolioReturn { get; set; }
        
        /// <summary>
        /// Tail risk at terminal point at Day 260 with 1% ETL
        /// </summary>
        public double ETL { get; set; }
        
        /// <summary>
        /// Alternative tail risk
        /// </summary>
        public double MaxETL { get; set; }
    }
}