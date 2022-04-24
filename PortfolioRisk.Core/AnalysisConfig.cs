using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PortfolioRisk.Core.DataTypes;

namespace PortfolioRisk.Core
{
    public class AnalysisConfig
    {
        public double? TotalAllocation { get; set; }
        public List<string> Assets { get; set; }
        public List<AssetCurrency> AssetCurrencies { get; set; }
        public List<double> Weights { get; set; }
        public List<string> Factors { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public IAdvancedVisualProvider AdvancedVisualProvider { get; set; } = new EmptyVisualProvider(); // Provide an empty one to bypass automatic missing value check

        #region Accessor
        public Dictionary<string, AssetCurrency> CurrencyMapping =>
            Assets.Zip(AssetCurrencies).ToDictionary(p => p.First, p => p.Second);
        #endregion

        #region Helpers
        public bool ContainsMissingValue()
        {
            // Automatically get all defined and thus required parameters for the configuration
            // and check whether any property is missing proper values
            return GetType()
                .GetProperties() 
                .Select(p => p.GetValue(this) ?? null)
                .Any(v => v == null);
        }
        public void NormalizeWeights()
        {
            double total = Weights.Sum();
            Weights = Weights.Select(w => w / total).ToList();
        }
        #endregion
    }
}
