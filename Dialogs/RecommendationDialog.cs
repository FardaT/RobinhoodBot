using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using RobinhoodBot.Model;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;

namespace RobinhoodBot.Dialogs
{
    public class RecommendationDialog : CancelAndHelpDialog
    {


        private const string recommendationIntent = "buy";
        public RecommendationDialog()
        : base(nameof(RecommendationDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AssetTypeStepAsync,
                RecommendationAsync,

            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> AssetTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var recommendationDetails = (RecommendationDetails)stepContext.Options;

            if (recommendationDetails.AssetType == null)
            {
                var promptMessage = MessageFactory.Text($"What type of assets {recommendationIntent} are you interested in?", InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(recommendationDetails.AssetType, cancellationToken);

        }
        private async Task<DialogTurnResult> RecommendationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var recommendationDetails = (RecommendationDetails)stepContext.Options;
            recommendationDetails.AssetType = (string)stepContext.Result;

            string[] AvailableAssets = { "stock", "stocks", "share", "shares" };


            if (!AvailableAssets.Contains(recommendationDetails.AssetType.ToLower()))
            { 
                var promptMessage = MessageFactory.Text($"Sorry, but our recommendation services are only available with equities", InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
            else
            {

                var RecommendationObject = new StockDataRequest();
                var recommendation = await RecommendationObject.GetBuyStockRecommendationAsync();

                var promptMessage = MessageFactory.Text(@$"Our top pick for you is {recommendation.Company} - ({recommendation.Ticker}) at the market price of {recommendation.ClosingPrice.ToString("C", CultureInfo.CreateSpecificCulture("en-US"))})", InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
        }


    }
}