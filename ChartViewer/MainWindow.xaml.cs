using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveChartsCore;
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
            if (App!.Series != null)
            {
                KeyValuePair<string, double[][]> first = App.Series.First();
                
                SeriesNames = App.Series.Keys.ToArray();
                Series = GetSeriesFor(first.Key, first.Value);
            }
            else
            {
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = new double[] {2, 1, 3, 5, 3, 4, 6},
                        Fill = null
                    }
                };
            }
        }
        private static LineSeries<double>[] GetSeriesFor(string name, double[][] series)
        {
            return series.Select(s => new LineSeries<double>()
            {
                Values = s,
                Fill = null,
                Name = name
            }).ToArray();
        }
        #endregion

        #region Public View Properties
        private string[] _seriesNames;
        public string[] SeriesNames { get => _seriesNames; set => SetField(ref _seriesNames, value); }
        private ISeries[] _series;
        public ISeries[] Series { get => _series; set => SetField(ref _series, value); }
        #endregion
        
        #region Events
        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selection = e.AddedItems[0].ToString();
            Series = GetSeriesFor(selection, App.Series[selection]);
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