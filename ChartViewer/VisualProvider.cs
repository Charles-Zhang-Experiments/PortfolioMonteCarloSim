using System.Linq;
using PortfolioRisk.Core;
using PortfolioRisk.Core.DataTypes;

namespace ChartViewer
{
    public class VisualProvider: IAdvancedVisualProvider
    {
        #region Interface
        public void ViewReport(Report report)
        {
            App app = new App(report);
            app.Run(new MainWindow());
        }
        #endregion
    }
}