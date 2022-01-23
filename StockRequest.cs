using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RobinhoodBot
{
    public class StockDataResults
    {
        public List<Result> Results { get; set; }
    }
    public class Result
    {
        public string T { get; set; }
        public double Vw { get; set; }
        public double C { get; set; }
        public long t { get; set; }
    }
    public class Recommendation
    {
        public string Ticker { get; set; }
        public string? Company { get; set; }
        public double ClosingPrice { get; set; }
    }

    public class StockDataRequest 
    {
        //private readonly IConfiguration _configuration;
        //public StockDataRequest(IConfiguration configuration)
        //{
        //    _configuration = configuration;
        //}
        private async Task<StockDataResults> GetStockDataAsync()
        {
            HttpClient client = new HttpClient();

            //string polygonapiKey = _configuration.GetValue<string>("polygonapiKey");
            string polygonapiKey = "OrfEKVnCn6pdDEe_pi3uNFwWQ5kd5Mqa";
            string polygonDate = DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd");
            string polygonUri = $"https://api.polygon.io/v2/aggs/grouped/locale/us/market/stocks/{polygonDate}?adjusted=true&apiKey={polygonapiKey}";
            string response = await client.GetStringAsync(polygonUri);

            if (response.Length > 0)
            {
                var stockdata = JsonConvert.DeserializeObject<StockDataResults>(response);
                return stockdata;
            }
            else
                throw new HttpRequestException("Something went wrong while fulfilling request. Please try again later or use our other services.");
        }

        private static string CompanyNameFinder(string ticker)
        {
            var dict_ticker_company_name = new Dictionary<string, string>();
            var company_names = File.ReadLines(Path.Combine(Environment.CurrentDirectory, "nasdaq.csv"));

            foreach (var line in company_names)
            {
                dict_ticker_company_name.Add(line.Split(',')[0], line.Split(',')[1]);
            }
            if (dict_ticker_company_name.ContainsKey(ticker))

                return dict_ticker_company_name[ticker];
            return "";
        }
        public async Task<Recommendation> GetBuyStockRecommendationAsync()
        {
            Recommendation recommendation = new Recommendation();

            var data = await GetStockDataAsync();

            var maxWeightedVolumeStocks = data.Results.OrderByDescending(r => r.Vw).FirstOrDefault();

            recommendation.Ticker = maxWeightedVolumeStocks.T;
            recommendation.ClosingPrice = maxWeightedVolumeStocks.C;
            recommendation.Company = CompanyNameFinder(maxWeightedVolumeStocks.T);

            return recommendation;
        }

        public async Task<Recommendation> GetSellStockRecommendationAsync()
        {
            Recommendation recommendation = new Recommendation();

            var data = await GetStockDataAsync();

            var minWeightedVolumeStocks = data.Results.OrderByDescending(r => r.Vw).FirstOrDefault();

            recommendation.Ticker = minWeightedVolumeStocks.T;
            recommendation.ClosingPrice = minWeightedVolumeStocks.C;
            recommendation.Company = CompanyNameFinder(minWeightedVolumeStocks.T);

            return recommendation;

        }
    }
}
