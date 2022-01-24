using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using RobinhoodBot.Model;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Collections.Generic;

namespace RobinhoodBot.Dialogs
{
    public class RecommendationDialog : CancelAndHelpDialog
    {
        public string recommendedTicker { get; set; }
        public double recommendedPrice { get; set; }

        private const string recommendationIntent = "buy";
        public RecommendationDialog()
        : base(nameof(RecommendationDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AssetTypeStepAsync,
                RecommendationAsync,
                FinalStepAsync,

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
                recommendedTicker = recommendation.Ticker;
                recommendedPrice = recommendation.ClosingPrice;


                //                var promptMessage = MessageFactory.Text(@$"Our top pick for you is {recommendation.Company} - ({recommendation.Ticker}) 
                //at the market price of {recommendation.ClosingPrice.ToString("C", CultureInfo.CreateSpecificCulture("en-US"))})", InputHints.ExpectingInput);
                //                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text(@$"Our top pick for you is {recommendation.Company} - ({recommendation.Ticker}) 
                    at the market price of {recommendation.ClosingPrice.ToString("C", CultureInfo.CreateSpecificCulture("en-US"))})
                    Would you like to add this stock to your portfolio?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Yes", "No" }),
                }, cancellationToken);
            }

        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.Activity.Text == "No")
            {
                var messageText = "Thank you for choosing our service";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = message }, cancellationToken);

            }
            else
            {
                var tradeDetails = new TradeDetails()
                {
                    AssetName = recommendedTicker,
                    Price = recommendedPrice.ToString("C", CultureInfo.CreateSpecificCulture("en-US")),
                    AssetType = "stock"
                };
                return await stepContext.ReplaceDialogAsync(nameof(TradeDialog), cancellationToken);
                //private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
                //{
                //    if (stepContext.Result == "Yes")    
                //    return await stepContext.NextAsync(recommendationDetails.AssetType, cancellationToken);

            }
        }

    }
}