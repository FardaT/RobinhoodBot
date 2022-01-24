
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using RobinhoodBot.LanguageServices;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RobinhoodBot.Model;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Collections.Generic;

namespace RobinhoodBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly StockTradingRecognizer _luisRecognizer;
        protected readonly ILogger Logger;


        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(StockTradingRecognizer luisRecognizer, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {

            Logger = logger;
            _luisRecognizer = luisRecognizer;


            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new RecommendationDialog());
            AddDialog(new TradeDialog());
            //AddDialog(new QuestionAnsweringDialog());
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
                EndingStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }


        // Figure out the user's intent and run the appropriate dialog to act on it
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            return await stepContext.NextAsync(null, cancellationToken);
        }


        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // Call CLU to gather info and identify intent 
            var luisResult = await _luisRecognizer.RecognizeAsync<StockMarketTopMovers>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case StockMarketTopMovers.Intent.AskForBuyRecommendation:
                case StockMarketTopMovers.Intent.AskForSellRecommendation:
                    
                    var recommendationDetails = new RecommendationDetails();
                    {
                        recommendationDetails.AssetType = luisResult.Entities?.asset_type?.FirstOrDefault()?.FirstOrDefault();
                    }

                    return await stepContext.BeginDialogAsync(nameof(RecommendationDialog), recommendationDetails, cancellationToken);

                case StockMarketTopMovers.Intent.BuyAssets:
                case StockMarketTopMovers.Intent.SellAssets:
                    var tradeDetails = new TradeDetails()
                    {
                        AssetName = luisResult.Entities?.purchase_details?.FirstOrDefault().asset.FirstOrDefault()?.asset_name?.FirstOrDefault(),
                        NumberOfAssets = luisResult.Entities?.purchase_details?.FirstOrDefault().number_of_stocks?.FirstOrDefault(),
                        Price = luisResult.Entities?.purchase_details?.FirstOrDefault().price?.FirstOrDefault(),
                        AssetType = luisResult?.Entities?.asset_type?.FirstOrDefault()?.FirstOrDefault(),
                    };

                    return await stepContext.BeginDialogAsync(nameof(TradeDialog), tradeDetails, cancellationToken);

                default:
                    // call QA if no match for LUIS
                    var questionAnswering = new QuestionAnswering();
                    var response = await questionAnswering.QuestionAnswerAsync(stepContext.Context.Activity.Text);

                    // QA answers if possible and prompts for another question
                    if (response.Value?.Answers?.Count > 0)
                    {
                        var answer = response.Value?.Answers.FirstOrDefault().Answer;
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);
                        break;
                        //return await stepContext.NextAsync(null, cancellationToken);
                        //return await stepContext.PromptAsync(nameof(ChoicePrompt),
                        //new PromptOptions
                        //{
                        //    Prompt = MessageFactory.Text("Would you like to know anything else?"),
                        //    Choices = ChoiceFactory.ToChoices(new List<string> { "Yes", "No"}),
                        //}, cancellationToken);

                        //public static async Task<DialogTurnResult> RestartStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
                        //{

                        //}





                    }
                    else
                    // If no match for either QA or LUIS
                    {
                        var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way";
                        var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                        await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                        break;
                    }

            }

            return await stepContext.NextAsync(null, cancellationToken);
        }


        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // In case of cancellation
            // the Result here will be null.
            if (stepContext.Result is TradeDetails result)
            {
                // Now we have all the booking details call the booking service.

                // If the call to the booking service was successful tell the user.

                var messageText = "Thank you for choosing our service";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            //var promptMessage = "What else can We do for you?";

            return await stepContext.PromptAsync(nameof(ChoicePrompt),
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Would you like to know anything else?"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Yes", "No" }),
            }, cancellationToken);
            //return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
        private async Task<DialogTurnResult> EndingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.Activity.Text == "No")
            {
                var messageText = "Thank you for choosing our service";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = message }, cancellationToken);

            }
            else
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, cancellationToken);
            }
        }
    }
}
