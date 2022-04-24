using System;
using System.Collections.Generic;

namespace ChartViewer
{
    public class ViewModel
    {
        #region Constructor

        public ViewModel()
        {
        }

        public ViewModel(Dictionary<string, double[][]> series)
        {
            Series = series;
        }
        #endregion

        #region Data
        public Dictionary<string, double[][]> Series { get; set; }
        public Dictionary<string, double> ETL { get; set; }
        public Dictionary<string, double> MaxETL { get; set; }
        public Dictionary<string,double> CurrentPrices { get; set; }
        public DateTime PriceDate { get; set; }
        #endregion
    }
}