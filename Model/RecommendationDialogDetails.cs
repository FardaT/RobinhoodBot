namespace RobinhoodBot.Model
{
    public class RecommendationDialogDetails
    {
        //Get Entity with all the information
        public StockMarketCognitiveModel RecommendationLuisResult { get; set; }
        //Extract details from Cognitive result for easier access (later on if needed)
        public string AssetType { get; set; }

        public string RecommendationIntent { get; set; }


        // Get stockrecommendation data
        public string RecommendedTicker { get; set; }

        public double RecommendedPrice { get; set; }
    }

   
}
