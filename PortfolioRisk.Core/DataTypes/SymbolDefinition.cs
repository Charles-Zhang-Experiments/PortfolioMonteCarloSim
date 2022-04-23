using System;

namespace PortfolioRisk.Core.DataTypes
{
    public class SymbolDefinition
    {
        public string Name { get; set; }
        public DateTime QueryStartDate { get; set; }
        public DateTime QueryEndDate { get; set; }   
    }
}