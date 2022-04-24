using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore.SkiaSharpView;

namespace ChartViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Constructor
        public MainWindow()
        {
            InitializeViewData();

            InitializeComponent();
        }
        private App App => Application.Current as App;
        #endregion

        #region Routine
        private void InitializeViewData()
        {
            if (App.Model != null)
            {
                (string symbol, double[][] scenarios) = App.Model.Series.First();
                
                SeriesNames = App.Model.Series.Keys.ToArray();
                Series = GetSeriesFor(symbol, scenarios);
                DynamicTitle = $"Visualization ({string.Join(", ", _seriesNames)})";
                SummaryText = BuildSummaryText(App.Model);
            }
            else
            {
                Series = new LineSeries<double>[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] {2, 1, 3, 5, 3, 4, 6},
                        Fill = null
                    }
                };
                DynamicTitle = "Visualization";
            }
        }
        private static LineSeries<double>[] GetSeriesFor(string name, double[][] series)
        {
            return series.Select((s, i) => new LineSeries<double>()
            {
                Values = s.Select(v => Math.Round(v, 2)),   // Round the values to 2 decimal places for readability
                Fill = null,
                Name = $"Scenario {i + 1}",
                GeometrySize = 2,
            }).ToArray();
        }
        private string BuildSummaryText(ViewModel viewModel)
        {
            StringBuilder builder = new StringBuilder();
            // ETL
            builder.AppendLine("ETL:");
            foreach ((string symbol, double etl) in viewModel.ETL) 
                builder.AppendLine($"  {symbol}: {etl}");
            // Max ETL
            builder.AppendLine("Max ETL:");
            foreach ((string symbol, double maxEtl) in viewModel.MaxETL)
                builder.AppendLine($"  {symbol}: {maxEtl}");
            // Current Prices
            builder.AppendLine($"Current Price: {string.Join(", ", viewModel.CurrentPrices.Select(cp => $"{cp.Key}: {cp.Value}"))}");

            return builder.ToString();
        }
        #endregion

        #region Public View Properties
        private string _dynamicTitle;
        public string DynamicTitle { get => _dynamicTitle; private set => SetField(ref _dynamicTitle, value); }
        private string _summaryText;
        public string SummaryText { get => _summaryText; private set => SetField(ref _summaryText, value); }
        private string[] _seriesNames;
        public string[] SeriesNames { get => _seriesNames; private set => SetField(ref _seriesNames, value); }
        private LineSeries<double>[] _series;
        public LineSeries<double>[] Series { get => _series; set => SetField(ref _series, value); }
        #endregion
        
        #region Events
        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0)
            {
                string selection = e.AddedItems[0]?.ToString();
                Series = GetSeriesFor(selection, App.Model.Series[selection]);
            }
        }
        #endregion

        #region Data Binding
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private bool SetField<TType>(ref TType field, TType value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<TType>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}