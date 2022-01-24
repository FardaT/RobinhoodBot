//using Microsoft.Bot.Builder;
//using Microsoft.Bot.Builder.Dialogs;
//using Microsoft.Bot.Schema;
//using RobinhoodBot.Model;
//using System.Threading;
//using System.Threading.Tasks;
//using RobinhoodBot.LanguageServices;

//namespace RobinhoodBot.Dialogs
//{
//    public class QuestionAnsweringDialog : CancelAndHelpDialog
//    {
//        public QuestionAnsweringDialog()
//        : base(nameof(QuestionAnsweringDialog))
//        {
//            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
//            {
//                QAIntrostepAsync,
//                QAPrompt,

//            }));

//            // The initial child Dialog to run.
//            InitialDialogId = nameof(WaterfallDialog);
//        }

//        private async Task<DialogTurnResult> QAIntrostepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
//        {
//            var response = await questionAnswering.QuestionAnswerAsync(stepContext.Context.Activity.Text);

//            if (response.Value?.Answers?.Count > 0)
//            {
//                return await stepContext.BeginDialogAsync(nameof(QuestionAnsweringDialog), cancellationToken);
//                //var answer = response.Value?.Answers.FirstOrDefault().Answer;
//                //await stepContext.Context.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);






//                var recommendationDetails = (RecommendationDetails)stepContext.Options;

//            if (recommendationDetails.AssetType == null)
//            {
//                var promptMessage = MessageFactory.Text($"What type of assets {recommendationIntent} are you interested in?", InputHints.ExpectingInput);
//                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
//            }

//            return await stepContext.NextAsync(recommendationDetails.AssetType, cancellationToken);

//        }
//    }
//}
