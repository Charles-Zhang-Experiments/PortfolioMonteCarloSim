using System.Linq;
using PortfolioRisk.Core;
using PortfolioRisk.Core.DataTypes;

namespace ChartViewer
{
    public class VisualProvider: IAdvancedVisualProvider
    {
        #region Configuration
        /// <summary>
        /// Don't draw all 5000 because the UI can't handle it
        /// </summary>
        private const int VisualizationSampleSize = 10;
        #endregion

        #region Interface
        public void ViewReport(Report report)
        {
            App app = new App(new ViewModel()
            {
                Series = report.PortfolioReturn.ToDictionary(pr => pr.Asset, pr => pr.Values.Take(VisualizationSampleSize).ToArray()),
                ETL = report.ETL,
                MaxETL = report.MaxETL,
                CurrentPrices = report.CurrentPrices,
                PriceDate = report.PriceDate
            });
            app.Run(new MainWindow());
        }
        #endregion
    }
}