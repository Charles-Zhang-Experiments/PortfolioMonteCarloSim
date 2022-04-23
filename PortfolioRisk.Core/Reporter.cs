using System;
using System.Collections.Generic;
using PortfolioRisk.Core.DataTypes;

namespace PortfolioRisk.Core
{
    public class Reporter
    {
        #region Constructor
        public Reporter(List<Dictionary<string,double[]>> results)
        {
            SimulationResults = results;
        }
        #endregion

        #region Private Members
        private List<Dictionary<string,double[]>> SimulationResults { get; }
        #endregion

        #region Public Interface Method
        public Report BuildReport(Dictionary<string, AssetCurrency> annotateAssetCurrency)
        {
            throw new NotImplementedException();
        }

        public void AnnouceReport(Report report)
        {
            // Basic stats
            Console.WriteLine(report.PortfolioReturn);
            Console.WriteLine(report.ETL);
            Console.WriteLine(report.MaxETL);
            
            // Analysis and trends
        }
        #endregion
    }
}