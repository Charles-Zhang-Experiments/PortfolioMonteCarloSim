using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioBuilder.Models;
using PortfolioRisk.Core;
using PortfolioRisk.Core.DataTypes;

namespace PortfolioBuilder
{
    public class ApplicationState
    {
        #region View Binding
        public static HashSet<AssetDefinition> Assets { get; set; } = new HashSet<AssetDefinition>();
        public static HashSet<AssetDefinition> Factors { get; set; } = new HashSet<AssetDefinition>();
        #endregion
        
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

        #region Routines
        public static void RefreshConfig()
        {
            ApplicationState.Config.Assets = ApplicationState.Assets.Select(a => a.Symbol).ToList();
            ApplicationState.Config.Factors = ApplicationState.Factors.Select(f => f.Symbol).ToList();
            ApplicationState.Config.Weights = ApplicationState.Assets.Select(a => a.Weight).ToList();
            ApplicationState.Config.NormalizeWeights();
        }
        #endregion
    }
}