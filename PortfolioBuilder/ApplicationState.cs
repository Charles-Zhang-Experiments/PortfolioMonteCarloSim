using System;
using PortfolioRisk.Core;
using PortfolioRisk.Core.DataTypes;

namespace PortfolioBuilder
{
    public class ApplicationState
    {
        #region Program State
        public static readonly AnalysisConfig Config = new AnalysisConfig()
        {
            TotalAllocation = 2000000000, // In CAD
            StartDate = new DateTime(2017, 1, 1),
            EndDate = new DateTime(2021, 12, 31),
        };
        public static PortfolioAnalyzer PortfolioAnalyzer { get; set; }
        public static Report Report { get; set; }
        #endregion
    }
}