using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortfolioRisk.Core;
using PortfolioRisk.Core.DataTypes;

namespace PortfolioBuilderWebApp
{
    public class PortfolioBuilder
    {
        #region Program State
        public static readonly AnalysisConfig Config = new AnalysisConfig()
        {
            TotalAllocation = 2000000000, // In CAD
            StartDate = new DateTime(2017, 1, 1),
            EndDate = new DateTime(2021, 12, 31),
        };
        public static PortfolioAnalyzer PortfolioAnalyzer { get; set; }
        public static Report Report { get; set; }
        #endregion

        #region Program Entrance
        public static async Task Main(string[] args)
        {
            WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(
                sp => new HttpClient {BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)});

            await builder.Build().RunAsync();
        }
        #endregion
    }
}