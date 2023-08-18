using System;
using System.Collections.Generic;
using System.Linq;
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

        #region Private Members
        private List<Dictionary<string,double[]>> SimulationResults { get; }
        private Dictionary<string, double> CurrentPrices { get; }
        private DateTime PriceDate { get; }
        #endregion

        #region Public Interface Method
        public Report BuildReport(AnalysisConfig config,
            Dictionary<string, AssetCurrency> annotateAssetCurrency, PortfolioAnalyzer analyzer)
        {
            Report report = new Report(CurrentPrices, PriceDate){ Analyzer = analyzer };
            
            NormalizeCurrencyForPnL(config, annotateAssetCurrency, report);
            ComputeOriginalInvestmentSize(config, report);
            ComputeETL(config, report);
            ComputeMaxETL(config, report);

            return report;
        }
        public string AnnounceReport(AnalysisConfig config, Report report)
        {
            // Basic stats
            return report.BuildSummaryText();
        }
        #endregion

        #region Routines
        private static void ComputeOriginalInvestmentSize(AnalysisConfig config, Report report)
        {
            // Size of each asset
            report.InvestmentSize = report.PortfolioReturn.ToDictionary(
                pnl => pnl.Asset,
                pnl => config.TotalAllocation!.Value * config.Weights[config.Assets.IndexOf(pnl.Asset)]);
            
            // Size of currency
            report.CurrencySize = new Dictionary<AssetCurrency, double>();
            Dictionary<string, AssetCurrency> mapping = config.CurrencyMapping;
            foreach (AssetCurrency currency in config.AssetCurrencies.Distinct())
            {
                if (currency == AssetCurrency.USD_TO_CAD)
                    throw new InvalidOperationException("Unexpected asset type");
                
                report.CurrencySize.Add(
                    currency, 
                    report.InvestmentSize
                        .Where(i => mapping[i.Key] == currency)
                        .Select(i => i.Value).Sum());
            }
        }
        private void ComputeETL(AnalysisConfig config, Report report)
        {
            report.ETL = report.PortfolioReturn.ToDictionary(pnl => pnl.Asset, pnl => 
                ETL(pnl.Values) / CurrentPrices[pnl.Asset] * report.InvestmentSize[pnl.Asset]);

            PnL usdToCad = report.FactorReturn.Single(fr => fr.Type == PnLType.ExchangeRate);
            double fx = ETL(usdToCad.Values) / CurrentPrices[usdToCad.Asset] * report.CurrencySize[AssetCurrency.USD];
            Dictionary<string, double> assetFactors = report.FactorReturn
                .Where(fr => fr.Type == PnLType.Asset)
                .ToDictionary(
                    fr => fr.Asset, 
                    fr => ETL(fr.Values) / CurrentPrices[fr.Asset] * report.InvestmentSize[fr.Asset]);
            report.ETLFxFactors = assetFactors.ToDictionary(af => af.Key, af => new FXFactor()
            {
                Self = af.Value,
                FX = fx
            });
            
            static double ETL(double[][] scenarios)
                => scenarios.Select(pv => pv.Last()).OrderBy(d => d)
                    .Take(PortfolioAnalyzer.ETLWorstCaseTake)
                    .Average();
        }
        private void ComputeMaxETL(AnalysisConfig config, Report report)
        {
            report.MaxETL = report.PortfolioReturn.ToDictionary(pnl => pnl.Asset, pnl =>
                MaxETL(pnl.Values) / CurrentPrices[pnl.Asset]  * config.TotalAllocation!.Value * config.Weights[config.Assets.IndexOf(pnl.Asset)]);
            
            PnL usdToCad = report.FactorReturn.Single(fr => fr.Type == PnLType.ExchangeRate);
            double fx = MaxETL(usdToCad.Values) / CurrentPrices[usdToCad.Asset] * report.CurrencySize[AssetCurrency.USD];
            Dictionary<string, double> assetFactors = report.FactorReturn
                .Where(fr => fr.Type == PnLType.Asset)
                .ToDictionary(
                    fr => fr.Asset, 
                    fr => MaxETL(fr.Values) / CurrentPrices[fr.Asset] * report.InvestmentSize[fr.Asset]);
            report.MaxETLFxFactors = assetFactors.ToDictionary(af => af.Key, af => new FXFactor()
            {
                Self = af.Value,
                FX = fx
            });

            static double MaxETL(double[][] scenarios)
                => scenarios.Select(pv => pv.Min()).OrderBy(d => d)
                    .Take(PortfolioAnalyzer.ETLWorstCaseTake)
                    .Average();
        }
        private void NormalizeCurrencyForPnL(AnalysisConfig analysisConfig, IReadOnlyDictionary<string, AssetCurrency> annotateAssetCurrency, Report report)
        {
            foreach (string asset in analysisConfig.Assets)
            {
                switch (annotateAssetCurrency[asset])
                {
                    case AssetCurrency.USD:
                    {
                        double[][] pnl = SimulationResults.Select(sr => sr[asset]).ToArray();
                        
                        // Report self as factor
                        report.FactorReturn.Add(new PnL()
                        {
                            Asset = asset,
                            Values = ElementWiseMultiply(pnl, CurrentPrices[asset]), // This is the price of the asset if FX rate is not changing,
                            Type = PnLType.Asset
                        });
                        
                        // Find available converter
                        string converter = annotateAssetCurrency.Single(ac => ac.Value == AssetCurrency.USD_TO_CAD).Key;
                        double[][] exchangeRate = SimulationResults.Select(sr => sr[converter]).ToArray();
                        
                        // Report Exchange Rate as factor
                        if (report.FactorReturn.All(fr => fr.Asset != converter))
                        {
                            report.FactorReturn.Add(new PnL()
                            {
                                Asset = converter,
                                Values = ElementWiseMultiply(exchangeRate, CurrentPrices[converter]),    // This is the price of FX irrelevant of underlying asset
                                Type = PnLType.ExchangeRate
                            });
                        }
                        
                        // Convert to CAD
                        report.PortfolioReturn.Add(new PnL()
                        {
                            Asset = asset,
                            Values = ElementWiseMultiply(pnl, exchangeRate, CurrentPrices[asset] / CurrentPrices[converter]),
                            Type = PnLType.Asset
                        });
                        break;
                    }
                    case AssetCurrency.CAD:
                    {
                        double[][] pnl = SimulationResults.Select(sr => sr[asset]).ToArray();
                        
                        // Convert return to dollar value
                        report.PortfolioReturn.Add(new PnL()
                        {
                            Asset = asset,
                            Values = ElementWiseMultiply(pnl, CurrentPrices[asset]),
                            Type = PnLType.Asset
                        });
                        break;
                    }
                    case AssetCurrency.USD_TO_CAD:
                        throw new ArgumentException("Wrong asset type.");
                    default:
                        throw new ArgumentOutOfRangeException($"Unknown type: {annotateAssetCurrency[asset]}");
                }
            }
        }
        #endregion

        #region Helpers
        private static double[][] ElementWiseMultiply(double[][] pnl, double baseRate)
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
        private static double[][] ElementWiseMultiply(double[][] pnl, double[][] exchangeRate, double currencyBaseRate)
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