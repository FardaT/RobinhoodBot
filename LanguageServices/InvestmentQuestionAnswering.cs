using Azure.AI.Language.QuestionAnswering;
using Azure;
using System;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder;

namespace RobinhoodBot.LanguageServices
{
    public class InvestmentQuestionAnswering : IQuestionAnswering
    {
        private QuestionAnsweringClient _client;
        private readonly QuestionAnsweringProject _project;
        public InvestmentQuestionAnswering(IConfiguration configuration)
        {
            Uri endpoint = new Uri("https://" + configuration["LuisAPIHostName"]);
            AzureKeyCredential credential = new AzureKeyCredential(configuration["QuestionAnswearingKey"]);

            _client = new QuestionAnsweringClient(endpoint, credential);

            _project = new QuestionAnsweringProject("InvestmentQnA", "production");

        }

        public async Task<Response<AnswersResult>> QuestionAnswerAsync(string question)
        {
            Response<AnswersResult> response = await _client.GetAnswersAsync(question, _project);
            foreach (var answer in response.Value.Answers)
                if (answer.Confidence > 0.3)
                    return response;

            return null;

        }


    }
}
