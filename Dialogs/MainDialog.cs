
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
        private readonly InvestmentQuestionAnswering _investmentQuestionAnswering;
        protected readonly ILogger Logger;


        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(StockTradingRecognizer luisRecognizer, InvestmentQuestionAnswering investmentQuestionAnswering, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {

            Logger = logger;
            _luisRecognizer = luisRecognizer;
            _investmentQuestionAnswering = investmentQuestionAnswering; 


            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new RecommendationDialog()); 
            AddDialog(new TradeDialog());
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                ConfirmationStepAsync,
                RestartStepAsync,
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
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file."), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }


        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // Call CLU to gather info and identify intent 
            var luisResult = await _luisRecognizer.RecognizeAsync<StockMarketCognitiveModel>(stepContext.Context, cancellationToken);

            switch (luisResult.TopIntent().intent)
            {
                //Initiate RecommendationDialog, logic whether it is a buy or sell implemented within the dialog
                case StockMarketCognitiveModel.Intent.AskForBuyRecommendation:
                case StockMarketCognitiveModel.Intent.AskForSellRecommendation:

                    var recommendationLuisResult = new RecommendationDialogDetails
                    {
                        //hand over luis result
                        RecommendationLuisResult = luisResult
                    };

                    return await stepContext.BeginDialogAsync(nameof(RecommendationDialog), recommendationLuisResult, cancellationToken);

                //Initiate TradeDialog, logic whether it is a buy or sell implemented within the dialog
                case StockMarketCognitiveModel.Intent.BuyAssets:
                case StockMarketCognitiveModel.Intent.SellAssets:
                    var tradeDetails = new TradeDetails()
                    {
                        //hand over luis result
                        TradeLuisResult = luisResult
                    };

                    return await stepContext.BeginDialogAsync(nameof(TradeDialog), tradeDetails, cancellationToken);

                //In case the intent is dismissive, proceed to next step
                //(rather used at reinstantiated main dialog instead of first run)

                case StockMarketCognitiveModel.Intent.Decline:
                    return await stepContext.NextAsync(luisResult.TopIntent().intent, cancellationToken);
                default:
                    // call QA if no match for LUIS
                    var response = await _investmentQuestionAnswering.QuestionAnswerAsync(stepContext.Context.Activity.Text);

                    // QA answers if possible and prompts for another question
                    if (response != null)
                    {
                        var answer = response.Value?.Answers.FirstOrDefault().Answer;
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);
                        break;

                    }
                    else
                    {
                        // If no match for either QA or LUIS go next step and prompt to rephrase

                        return await stepContext.NextAsync(null, cancellationToken);
                    }


            }

            return await stepContext.NextAsync(null, cancellationToken);
        }


        private async Task<DialogTurnResult> ConfirmationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext == Decline) // in this case if context is null, it had to come from decline intent so proceed to end dialog
            {
                var message = MessageFactory.Text("Thank you for choosing our service!", "Thank you for choosing our service!", InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken); //null for now but may change if want to store for future conversations
            }

            if ( )
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry, we could not catch that. Please repeat or rephrase your question.")}, cancellationToken);


            //Ask user if he/she wishes to proceed asking with a prompt - restart main dialog until dismissive intent is not recognized
            var promptMessage = MessageFactory.Text("What else can we help you with?", InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        //Restart main dialog with the question asked
        private async Task<DialogTurnResult> RestartStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.ReplaceDialogAsync(InitialDialogId, stepContext, cancellationToken);
        }
    }
}
