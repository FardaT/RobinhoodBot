using Azure;
using Azure.AI.Language.QuestionAnswering;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;

namespace RobinhoodBot.LanguageServices
{
    public interface IQuestionAnswering
    {
        Task<Response<AnswersResult>> QuestionAnswerAsync(string question);
    }
}
