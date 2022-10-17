using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using RobinhoodBot.Model;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Collections.Generic;
using RobinhoodBot.StockDataRequest;
using System;

namespace RobinhoodBot.Dialogs
{
    public class RecommendationDialog : CancelAndHelpDialog
    {
        private Recommendation recommendation;
        public RecommendationDialog()
        : base(nameof(RecommendationDialog))
        {
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt), ConfrimationStepValidatorAsync));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt), AssetTypeStepValidatorAsync));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AssetTypeStepAsync,
                RecommendationAsync,
                FinalStepAsync,

            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        public RecommendationDialogDetails recommendationDetails { get; set; }

        //Determine whether buy or sell intent based on luisrecognition and if asset type is missing, ask for it.
        private async Task<DialogTurnResult> AssetTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            recommendationDetails = (RecommendationDialogDetails)stepContext.Options;

            //check whether sell or buy intent and set flow accordingly
            //also initiate stockrequest accordingly

            try
            {
                if (recommendationDetails.RecommendationLuisResult.TopIntent().intent == StockMarketCognitiveModel.Intent.AskForBuyRecommendation)
                {
                    recommendationDetails.RecommendationIntent = "buy";
                    recommendation = await StockRequest.GetBuyStockRecommendationAsync();

                }

                else
                {
                    recommendationDetails.RecommendationIntent = "sell";
                    recommendation = await StockRequest.GetSellStockRecommendationAsync();
                }
            }
            catch (Exception)
            {

                var message = MessageFactory.Text("Sorry, something went wrong while trying to get your recommendations. Please try again later or use our other services.", InputHints.ExpectingInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            // If no match for asset_type
            recommendationDetails.AssetType = recommendationDetails.RecommendationLuisResult.Entities.asset_type?[0][0];
            if (recommendationDetails.AssetType == null)
            {
                return await stepContext.PromptAsync("AssetTypeStep",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"What type of assets do you wish to {recommendationDetails.RecommendationIntent}?"),
                    RetryPrompt = MessageFactory.Text($"Apologies, we did not catch what asset you wish to {recommendationDetails.RecommendationIntent}. Please choose from the options below."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Stock", "Bond", "Option", "Cryptocurrency" }),
                }, cancellationToken);
            }

            return await stepContext.NextAsync(recommendationDetails.AssetType, cancellationToken);
        }


        //Validate Choice of Asset type if no match 
        private async Task<bool> AssetTypeStepValidatorAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
                return await Task.FromResult(true);
            return false;
        }
        private async Task<DialogTurnResult> RecommendationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            recommendationDetails.AssetType = ((FoundChoice)stepContext.Result).Value;

            if (recommendationDetails.AssetType != "stock")
            {
                var message = MessageFactory.Text($"Sorry, but our recommendation services are only available with equities at the moment", InputHints.ExpectingInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                recommendationDetails.RecommendedTicker = recommendation.Ticker;
                recommendationDetails.RecommendedPrice = recommendation.ClosingPrice;

                return await stepContext.PromptAsync(nameof(ConfirmPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text(@$"Our top pick for you is {recommendation.Company} - ({recommendation.Ticker}) 
                    at the market price of {recommendation.ClosingPrice.ToString("C", CultureInfo.CreateSpecificCulture("en-US"))})
                    Would you like to add this stock to your portfolio?"),
                    RetryPrompt = MessageFactory.Text("Please select from the below options"),
                }, cancellationToken);
            }

        }

        //Validate if answer as yes or no
        private async Task<bool> ConfrimationStepValidatorAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
                return await Task.FromResult(true);
            return false;
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!(bool)stepContext.Result)
            {
                return await stepContext.EndDialogAsync(null, cancellationToken); //Return dialog result to main dialog just in case it needs for any further development

            }
            else
            {
                TradeDetails tradeDetails = new TradeDetails()
                {
                    // transfer the rest
                    TradeIntent = recommendationDetails.RecommendationIntent,
                    AssetName = recommendationDetails.RecommendedTicker,
                    AssetType = recommendationDetails.AssetType,
                    Price = recommendationDetails.RecommendedPrice.ToString(), //handle as str for now
                };
                return await stepContext.BeginDialogAsync(nameof(TradeDialog), tradeDetails, cancellationToken);
            }
        }

    }
}