using System;
using System.Collections.Generic;
using System.IO;

namespace RobinhoodBot.StockDataRequest
{
    public class CompanyNameHelpers
    {

        //Method to match Ticker symbol with Company Name
        public static string Finder(string ticker)
        {
            var dictTickerCompanyName = new Dictionary<string, string>();
            var companyNames = File.ReadLines(Path.Combine(Environment.CurrentDirectory, "StockDataRequest", "nasdaq.csv"));

            foreach (var line in companyNames)
            {
                dictTickerCompanyName.Add(line.Split(',')[0], line.Split(',')[1]);
            }
            if (dictTickerCompanyName.ContainsKey(ticker))

                return dictTickerCompanyName[ticker];
            return "";
        }


        //Method to split up input for company name, and check for matches in nasdaq.csv
        // ennél sokkal szofisztikáltabb dolgot lehetne írni de demonstrálás végett marad ez
        public static string Matcher(string input)
        {
            var utteranceWords = input.ToLower().Split(" ");
            var companyNames = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "StockDataRequest", "nasdaq.csv"));
            var companyUtterances = companyNames.ToLower().Split(',');

            foreach (var word in utteranceWords)
            {
                foreach (var company in companyUtterances)
                {
                    if ( word == company)
                        return company.ToUpper();   
                }
            }
            return "";
        }

    }
}
