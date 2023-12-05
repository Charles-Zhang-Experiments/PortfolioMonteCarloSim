using PortfolioRisk.Core;

namespace CoreAlgorithmTests
{
    public class YahooFinanceHelperTests
    {
        [Fact]
        public void YahooHelperShouldSaveFile()
        {
            string ticker = "AAPL";
            string currency = "USD";
            string filePath = Path.GetTempFileName();
            new YahooFinanceHelper().GetSymbol(new SymbolDefinition(ticker, DateTime.Today.AddMonths(-5 /*Remark-cz: Just long enough for some data points.*/), DateTime.Today, currency), YahooFinanceHelper.YahooTimeInterval.ThreeMonths, "Close", Path.Combine(Directory.GetCurrentDirectory(), filePath));
            Assert.True(File.Exists(filePath));
        }
    }
}