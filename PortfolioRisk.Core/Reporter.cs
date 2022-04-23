using System.Collections.Generic;
using PortfolioRisk.Core.DataTypes;

namespace PortfolioRisk.Core
{
    public class Reporter
    {
        public Reporter(List<Dictionary<string,double[]>> results)
        {
            SimulationResults = results;
        }

        public List<Dictionary<string,double[]>> SimulationResults { get; set; }

        public Report MakeReport()
        {
            throw new System.NotImplementedException();
        }
    }
}