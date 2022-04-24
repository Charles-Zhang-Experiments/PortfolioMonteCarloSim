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
        public Reporter(List<Dictionary<string, double[]>> results, Dictionary<string, double> currentPrices,
            DateTime priceDate)
        {
            SimulationResults = results;
            CurrentPrices = currentPrices;
            PriceDate = priceDate;
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Don't draw all 5000 because the UI can't handle it
        /// </summary>
        private const int VisualizationSampleSize = 10;
        #endregion

        #region Private Members
        private List<Dictionary<string,double[]>> SimulationResults { get; }
        private Dictionary<string, double> CurrentPrices { get; }
        private DateTime PriceDate { get; }
        #endregion

        #region Public Interface Method
        public Report BuildReport(AnalysisConfig config,
            Dictionary<string, AssetCurrency> annotateAssetCurrency)
        {
            Report report = new Report(CurrentPrices, PriceDate);
            
            NormalizeCurrencyForPnL(config, annotateAssetCurrency, report);
            ComputeETL(config, report);
            ComputeMaxETL(config, report);

            return report;
        }
        public void AnnounceReport(Report report)
        {
            // Basic stats
            // Current Price
            Console.WriteLine($"Current Price ({report.PriceDate:yyyy-MM-dd}): {string.Join(", ", report.CurrentPrices.Select(cp => $"{cp.Key}: {cp.Value:N2}"))}");
            // ETL
            Console.WriteLine("ETL:");
            foreach ((string symbol, double etl) in report.ETL) 
                Console.WriteLine($"  {symbol}: {(long)etl}");
            // Max ETL
            Console.WriteLine("Max ETL:");
            foreach ((string symbol, double maxEtl) in report.MaxETL)
                Console.WriteLine($"  {symbol}: {(long)maxEtl}");
            
            // Path visualization
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

        #region Routines
        private void ComputeETL(AnalysisConfig config, Report report)
        {
            report.ETL = report.PortfolioReturn.ToDictionary(pnl => pnl.Asset, pnl =>
                pnl.Values.Select(pv => pv.Last()).OrderBy(d => d)
                    .Take(PortfolioAnalyzer.ETLWorstCaseTake)
                    .Average() / CurrentPrices[pnl.Asset] * config.TotalAllocation!.Value * config.Weights[config.Assets.IndexOf(pnl.Asset)]);
        }
        private void ComputeMaxETL(AnalysisConfig config, Report report)
        {
            report.MaxETL = report.PortfolioReturn.ToDictionary(pnl => pnl.Asset, pnl =>
                pnl.Values.Select(pv => pv.Min()).OrderBy(d => d)
                    .Take(PortfolioAnalyzer.ETLWorstCaseTake)
                    .Average() / CurrentPrices[pnl.Asset]  * config.TotalAllocation!.Value * config.Weights[config.Assets.IndexOf(pnl.Asset)]);
        }
        private void NormalizeCurrencyForPnL(AnalysisConfig analysisConfig, IReadOnlyDictionary<string, AssetCurrency> annotateAssetCurrency, Report report)
        {
            foreach (string asset in analysisConfig.Assets)
            {
                double[][] pnl = SimulationResults.Select(sr => sr[asset]).ToArray();

                switch (annotateAssetCurrency[asset])
                {
                    case AssetCurrency.USD_TO_CAD:
                        throw new ArgumentException("Wrong asset type.");
                    case AssetCurrency.USD:
                    {
                        // Find available converter
                        string converter = annotateAssetCurrency.Single(ac => ac.Value == AssetCurrency.USD_TO_CAD).Key;
                        double[][] exchangeRate = SimulationResults.Select(sr => sr[converter]).ToArray();

                        // Convert to CAD
                        pnl = ElementWiseMultiply(pnl, exchangeRate, CurrentPrices[converter] * CurrentPrices[asset]);
                        break;
                    }
                    case AssetCurrency.CAD:
                        // Convert return to dollar value
                        pnl = ElementWiseMultiply(pnl, CurrentPrices[asset]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
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
        private double[][] ElementWiseMultiply(double[][] pnl, double baseRate)
        {
            if (pnl.Length != PortfolioAnalyzer.SimulationIterations
                || pnl.First().Length != HistoricalSimulation.YearReturnDays)
                throw new ArgumentException("Wrong PnL size!");

            // Prepare result matrix
            double[][] result = new double[PortfolioAnalyzer.SimulationIterations][];
            for (int i = 0; i < result.Length; i++)
                result[i] = new double[HistoricalSimulation.YearReturnDays];
            
            // Compute result matrix
            for (int scenario = 0; scenario < pnl.Length; scenario++)
            {
                for (int day = 0; day < pnl.First().Length; day++)
                {
                    result[scenario][day] = pnl[scenario][day] * baseRate;
                }
            }

            return result;
        }
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