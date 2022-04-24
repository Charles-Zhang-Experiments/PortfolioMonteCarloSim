using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PortfolioRisk.Core
{
    public class AnalysisConfig
    {
        public List<string> Assets { get; set; }
        public List<string> Factors { get; set; }
        public double? TotalAllocation { get; set; }
        public List<double> Weights { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

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
