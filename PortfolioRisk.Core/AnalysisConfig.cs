using System;
using System.Collections.Generic;
using System.Text;

namespace PortfolioRisk.Core
{
    public class AnalysisConfig
    {
        public List<string> Assets { get; set; }
        public List<string> Factors { get; set; }
        public double? TotalAllocation { get; set; }
        public List<double> Weights { get; set; }

    }
}
