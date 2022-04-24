using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using PortfolioRisk.Core.DataTypes;

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
        public App(Report report)
        {
            Report = report;
        }
        #endregion

        #region Data
        public Report Report { get; }
        #endregion
    }
}