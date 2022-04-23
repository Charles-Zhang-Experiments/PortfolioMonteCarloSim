using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ChartViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Constructor

        public App()
        {
        }

        public App(Dictionary<string, double[][]> series)
        {
            Series = series;
        }
        #endregion

        #region Data
        public Dictionary<string, double[][]> Series { get; set; }
        #endregion
    }
}