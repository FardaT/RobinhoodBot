
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using RobinhoodBot.LanguageServices;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RobinhoodBot.Model;
using Azure.AI.Language.QuestionAnswering;
using System;
using Azure;

namespace RobinhoodBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly StockTradingRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        public QuestionAnsweringClient client { get; private set; }

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(StockTradingRecognizer luisRecognizer, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            //Disrorderly management of QA 
            Uri endpoint = new Uri("https://westeurope.api.cognitive.microsoft.com/");
            AzureKeyCredential credential = new AzureKeyCredential("720d4f403e61473fa3b40e39538b9b6a");
            client = new QuestionAnsweringClient(endpoint, credential);
            Logger = logger;
            _luisRecognizer = luisRecognizer;


            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new RecommendationDialog());
            AddDialog(new TradeDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
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
            var messageText = stepContext.Options?.ToString() ?? "Welcome to Robinhood trading platform! How can we help you?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
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
                        AssetName = luisResult.Entities?.purchase_details?.FirstOrDefault().asset[1].asset_name?.FirstOrDefault(),
                        NumberOfAssets = luisResult.Entities?.purchase_details?.FirstOrDefault().number_of_stocks?.FirstOrDefault(),
                        Price = luisResult.Entities?.purchase_details?.FirstOrDefault().price?.FirstOrDefault(),
                        AssetType = luisResult?.Entities?.asset_type?.FirstOrDefault()?.FirstOrDefault(),
                    };

                    return await stepContext.BeginDialogAsync(nameof(TradeDialog), tradeDetails, cancellationToken);

                default:
                    //Call QuestionAnswering to see if it catches the question
                    string projectName = "InvestmentQnA";
                    string deploymentName = "production";
                    QuestionAnsweringProject project = new QuestionAnsweringProject(projectName, deploymentName);
                    Response<AnswersResult> response = await client.GetAnswersAsync(stepContext.Context.Activity.Text , project);
                    // Catch all for unhandled intents

                    if (response.Value?.Answers?.Count > 0)
                    {
                        var answer = response.Value?.Answers.FirstOrDefault().Answer;
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);
                    }
                    else
                    // If no match for either QA or LUIS
                    {
                        var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way";
                        var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                        await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                        break;
                    }
                    break;
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
            var promptMessage = "What else can We do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
