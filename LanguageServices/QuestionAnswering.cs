using Azure.AI.Language.QuestionAnswering;
using Azure;
using System;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RobinhoodBot.LanguageServices
{
    public class QuestionAnswering
    {
        private QuestionAnsweringClient client;
        private QuestionAnsweringClient QAClientInstance()
        {
            Uri endpoint = new Uri("https://westeurope.api.cognitive.microsoft.com/");
            AzureKeyCredential credential = new AzureKeyCredential("720d4f403e61473fa3b40e39538b9b6a");
            client = new QuestionAnsweringClient(endpoint, credential);
            return client;
        }

        private static QuestionAnsweringProject QAProjectinstance()
        {

            string projectName = "InvestmentQnA";
            string deploymentName = "production";
            QuestionAnsweringProject project = new QuestionAnsweringProject(projectName, deploymentName);
            return project; 
            
        }
        public async Task<Response<AnswersResult>> QuestionAnswerAsync(string stepContext)
        {
            Uri endpoint = new Uri("https://westeurope.api.cognitive.microsoft.com/");
            AzureKeyCredential credential = new AzureKeyCredential("720d4f403e61473fa3b40e39538b9b6a");
            client = new QuestionAnsweringClient(endpoint, credential);

            var instance = QAProjectinstance();
            Response<AnswersResult> response = await client.GetAnswersAsync(stepContext, instance);
            return response;

        }


    }
}
