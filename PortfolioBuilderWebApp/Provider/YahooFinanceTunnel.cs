using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace PortfolioBuilderWebApp.Provider
{
    public static class YahooFinanceTunnel
    {
        public static string TunnelNoCors(string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.SetBrowserRequestMode(BrowserRequestMode.NoCors);  
            request.SetBrowserRequestCache(BrowserRequestCache.NoStore); // optional            
            using HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = httpClient.Send(request);
            string content = response.Content.ReadAsStringAsync().Result;
            return content;
        }
    }
}