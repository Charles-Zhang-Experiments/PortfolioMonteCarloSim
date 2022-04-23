using System;
using System.Collections.Generic;
using System.Linq;
using ChartViewer;
using PortfolioRisk.Core.Algorithm;
using PortfolioRisk.Core.DataTypes;

namespace PortfolioRisk.Core
{
    public class Reporter
    {
        #region Constructor
        public Reporter(List<Dictionary<string, double[]>> results, Dictionary<string, double> currentPrices)
        {
            SimulationResults = results;
            CurrentPrices = currentPrices;
        }
        #endregion

        #region Private Members
        private List<Dictionary<string,double[]>> SimulationResults { get; }
        private Dictionary<string, double> CurrentPrices { get; }
        #endregion

        #region Public Interface Method
        public Report BuildReport(AnalysisConfig analysisConfig,
            Dictionary<string, AssetCurrency> annotateAssetCurrency)
        {
            Report report = new Report();
            
            NormalizeCurrencyForPnL(analysisConfig, annotateAssetCurrency, report);
            ComputeETL(report);
            ComputeMaxETL(report);

            return report;
        }
        public void AnnounceReport(Report report)
        {
            // Basic stats
            Console.WriteLine($"ETL: {report.ETL}");
            Console.WriteLine($"Max ETL: {report.MaxETL}");
            // Path visualization
            App app = new App(report.PortfolioReturn.ToDictionary(pr => pr.Asset, pr => pr.Values.Take(1).ToArray()));
            app.Run(new MainWindow());
        }
        #endregion

        #region Routines
        private void ComputeETL(Report report)
        {
            report.ETL = report.PortfolioReturn.ToDictionary(pnl => pnl.Asset, pnl =>
                pnl.Values.Select(pv => pv.Last()).OrderBy(d => d).Take(PortfolioAnalyzer.ETLWorstCaseTake).Average() * CurrentPrices[pnl.Asset]);
        }
        private void ComputeMaxETL(Report report)
        {
            report.MaxETL = report.PortfolioReturn.ToDictionary(pnl => pnl.Asset, pnl =>
                pnl.Values.Select(pv => pv.Min()).OrderBy(d => d).Take(PortfolioAnalyzer.ETLWorstCaseTake).Average() * CurrentPrices[pnl.Asset]);
        }
        private void NormalizeCurrencyForPnL(AnalysisConfig analysisConfig, Dictionary<string, AssetCurrency> annotateAssetCurrency, Report report)
        {
            foreach (string asset in analysisConfig.Assets)
            {
                double[][] pnl = SimulationResults.Select(sr => sr[asset]).ToArray();

                if (annotateAssetCurrency[asset] == AssetCurrency.USD_TO_CAD)
                    throw new ArgumentException("Wrong asset type.");
                if (annotateAssetCurrency[asset] == AssetCurrency.USD)
                {
                    // Find available converter
                    string converter = annotateAssetCurrency.Single(ac => ac.Value == AssetCurrency.USD_TO_CAD).Key;
                    double[][] exchangeRate = SimulationResults.Select(sr => sr[converter]).ToArray();

                    // Convert to CAD
                    pnl = ElementWiseMultiply(pnl, exchangeRate, CurrentPrices[converter]);
                }

                report.PortfolioReturn.Add(new PnL()
                {
                    Asset = asset,
                    Values = pnl
                });
            }
        }
        #endregion

        #region Helpers
        private double[][] ElementWiseMultiply(double[][] pnl, double[][] exchangeRate, double currencyBaseRate)
        {
            if (pnl.Length != exchangeRate.Length
                || pnl.First().Length != exchangeRate.First().Length)
                throw new ArgumentException("Mismatching and potentially wrong PnL size!");

            // Prepare result matrix
            double[][] result = new double[PortfolioAnalyzer.SimulationIterations][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new double[HistoricalSimulation.YearReturnDays];
            
            // Compute result matrix
            for (int scenario = 0; scenario < pnl.Length; scenario++)
            {
                for (int day = 0; day < pnl.First().Length; day++)
                {
                    result[scenario][day] = pnl[scenario][day] * exchangeRate[scenario][day] * currencyBaseRate;
                }
            }

            return result;
        }
        #endregion
    }
}