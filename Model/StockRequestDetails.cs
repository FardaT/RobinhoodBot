using System.Collections.Generic;

namespace RobinhoodBot.Model
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
        public string Company { get; set; }
        public double ClosingPrice { get; set; }
    }
}
