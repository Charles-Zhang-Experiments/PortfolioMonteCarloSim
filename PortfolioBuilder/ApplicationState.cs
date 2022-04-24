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
        public static void AddPortfolioAsset(AssetDefinition newAsset)
        {
            Assets.Add(newAsset);
            RefreshConfig();

            PortfolioAnalyzer = null;
            Report = null;
        }
        public static void RemovePortfolioAsset(AssetDefinition oldAsset)
        {
            Assets.Remove(oldAsset);
            RefreshConfig();
            
            PortfolioAnalyzer = null;
            Report = null;
        }
        public static void RefreshConfig()
        {
            PopulateFactors();
            
            Config.Assets = Assets.Select(a => a.Symbol).ToList();
            Config.AssetCurrencies = Assets.Select(a => a.Currency).ToList();
            Config.Factors = Factors.Select(f => f.Symbol).ToList();
            Config.Weights = Assets.Select(a => a.Weight).ToList();
            Config.NormalizeWeights();
        }

        private static void PopulateFactors()
        {
            Factors.Clear();
            foreach (AssetDefinition asset in Assets)
            {
                Factors.Add(asset);
                
                if (asset.Currency == AssetCurrency.USD && Factors.All(f => f.Currency != AssetCurrency.USD_TO_CAD))
                    Factors.Add(new AssetDefinition("USD/CAD", AssetCurrency.USD_TO_CAD, 0));
            }
        }

        #endregion
    }
}