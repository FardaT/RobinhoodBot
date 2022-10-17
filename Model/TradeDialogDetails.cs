namespace RobinhoodBot.Model
{
    public class TradeDetails
    {
        //Get Entity with all the information
        public StockMarketCognitiveModel TradeLuisResult { get; set; }

        public string TradeIntent { get; set; }

        public string AssetName { get; set; }
        public string AssetType { get; set; }
        public string NumberOfAssets { get; set; }
        public string Price { get; set; }
    }      

}
