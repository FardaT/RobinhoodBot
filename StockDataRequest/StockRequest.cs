using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using RobinhoodBot.Model;

namespace RobinhoodBot.StockDataRequest
{

    public class StockCall
    {
        private static readonly string _polygonUri;

        static StockCall()
        {
            var configuration = Startup.Configuration;
            string polygonapiKey = configuration["polygonapiKey"];
            string polygonDate = DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd") : DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            _polygonUri = $"https://api.polygon.io/v2/aggs/grouped/locale/us/market/stocks/{polygonDate}?adjusted=true&apiKey={polygonapiKey}";

        }
        public static async Task<StockDataResults> GetStockDataAsync()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(_polygonUri);

            if (response.IsSuccessStatusCode)
            {
                var stockdata = await response.Content.ReadAsAsync<StockDataResults>();
                return stockdata;
            }
            else
                throw new Exception(response.ReasonPhrase);
        } 
    }

    public class StockRequest
    {
        //Get buy recommendation 
        public static async Task<Recommendation> GetBuyStockRecommendationAsync()
        {
            Recommendation recommendation = new Recommendation();

            var data = await StockCall.GetStockDataAsync();

            var maxWeightedVolumeStocks = data.Results.OrderByDescending(r => r.Vw).FirstOrDefault();

            recommendation.Ticker = maxWeightedVolumeStocks.T;
            recommendation.ClosingPrice = maxWeightedVolumeStocks.C;
            recommendation.Company = CompanyNameHelpers.Finder(maxWeightedVolumeStocks.T);

            return recommendation;
        }
        //Get sell recommendation 
        public static async Task<Recommendation> GetSellStockRecommendationAsync()
        {
            Recommendation recommendation = new Recommendation();

            var data = await StockCall.GetStockDataAsync();

            var minWeightedVolumeStocks = data.Results.OrderByDescending(r => r.Vw).FirstOrDefault();

            recommendation.Ticker = minWeightedVolumeStocks.T;
            recommendation.ClosingPrice = minWeightedVolumeStocks.C;
            recommendation.Company = CompanyNameHelpers.Finder(minWeightedVolumeStocks.T);

            return recommendation;

        }

    }

}
