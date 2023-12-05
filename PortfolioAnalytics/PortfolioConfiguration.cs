namespace PortfolioAnalytics
{
    public record Asset(string Name, string Currency, double Weight);
    public class PortfolioConfiguration
    {
        #region Properties
        public double? TotalAllocation { get; set; }
        public Asset[] Assets { get; set; }
        /// <summary>
        /// For non-asset factors e.g. FX
        /// </summary>
        public string[] AdditionalFactors { get; set; }
        /// <summary>
        /// Factors are the decomposition components, includes both assets factors PLUS non-assets factors (FX or currencies);
        /// </summary>
        public IEnumerable<string> AllFactors => Assets.Select(a => a.Name).Union(AdditionalFactors);
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        #endregion

        #region Helpers
        public bool GetMissingValues(out IEnumerable<string> missingValueProperties)
        {
            var properties = GetType()
                .GetProperties().ToList();

            // Automatically get all defined and thus required parameters for the configuration
            // and check whether any property is missing proper values
            missingValueProperties = properties
                .Where(p => p.GetValue(this) == null)
                .Select(p => p.Name);

            return missingValueProperties.Any();
        }
        public void NormalizeWeights()
        {
            double total = Assets.Sum(a => a.Weight);
            Assets = Assets.Select(a => new Asset(a.Name, a.Currency, a.Weight / total)).ToArray();
        }
        #endregion
    }
}
