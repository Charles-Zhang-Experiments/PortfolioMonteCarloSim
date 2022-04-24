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
        public App(ViewModel model)
        {
            Model = model;
        }
        #endregion

        #region Data
        public ViewModel Model { get; }
        #endregion
    }
}