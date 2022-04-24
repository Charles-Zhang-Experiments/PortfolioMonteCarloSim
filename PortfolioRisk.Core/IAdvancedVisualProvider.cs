using PortfolioRisk.Core.DataTypes;

namespace PortfolioRisk.Core
{
    public interface IAdvancedVisualProvider
    {
        void ViewReport(Report report);
    }

    public class EmptyVisualProvider : IAdvancedVisualProvider
    {
        public void ViewReport(Report report)
        {
            return;
        }
    }
}