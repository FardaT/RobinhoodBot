using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using RobinhoodBot.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RobinhoodBot.StockDataRequest;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace RobinhoodBot.Dialogs
{
    public class TradeDialog : CancelAndHelpDialog
    {
        public TradeDialog()
        : base(nameof(TradeDialog))
            //Initialize dialogs
        {
            AddDialog(new ChoicePrompt("AssetTypeStep", AssetTypeStepValidatorAsync));
            AddDialog(new TextPrompt("AssetNameStep", AssetNameStepValidatorAsync));
            AddDialog(new ChoicePrompt("PriceConfirmation", PriceConfirmationStepValidatorAsync));
            AddDialog(new TextPrompt("LimitPriceConfirmationStep")); //needs validator with logic to recognize the sum
            AddDialog(new NumberPrompt<int>("NumberConfrimationStep", NumberConfrimationStepValidatorAsync));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt), ConfrimationStepValidatorAsync));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                InitialStepAsync,
                AssetTypeStepAsync,
                AssetNameStepAsync,
                PriceStepAsync,
                PriceConfirmationStepAsync,
                NumberConfrimationStepAsync,
                ConfirmationStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        public TradeDetails tradeDetails { get; set; }


        //Extract values got from initiator dialog
        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            tradeDetails = (TradeDetails)stepContext.Options;

            //Get inputs from luisresult if it was called from main dialog (otherwise this would overwrite results got from other initiator dialog)
            if (stepContext.Parent.Parent.ActiveDialog.Id == nameof(MainDialog))
            {
                var tradeEntities = tradeDetails.TradeLuisResult.Entities;

                tradeDetails.AssetName = tradeEntities.purchase_details?[0].asset?[0].asset_name?[0];
                tradeDetails.NumberOfAssets = tradeEntities.purchase_details?[0].number_of_stocks?[0];
                tradeDetails.Price = tradeEntities.purchase_details?[0].price?[0];
                tradeDetails.AssetType = tradeEntities.asset_type?[0][0];

                //Determine whether buy or sell intent based on top intent
                if (tradeDetails.TradeLuisResult.TopIntent().intent == StockMarketCognitiveModel.Intent.BuyAssets)
                    tradeDetails.TradeIntent = "buy";
                else
                    tradeDetails.TradeIntent = "sell";
            }

            return await stepContext.NextAsync(null, cancellationToken);

        }

        //Ask for Asset type if missing
        private async Task<DialogTurnResult> AssetTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (tradeDetails.AssetType == null)
            {
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"What type of assets do you wish to {tradeDetails.TradeIntent}?"),
                    RetryPrompt = MessageFactory.Text($"Apologies, we did not catch what asset you wish to {tradeDetails.TradeIntent}. Please choose from the options below."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Stock", "Bond", "Option", "Cryptocurrency" }),
                }, cancellationToken);
            }
            return await stepContext.NextAsync(tradeDetails.AssetType, cancellationToken);
        }

        //Validate Choice of Asset type if no match 
        private async Task<bool> AssetTypeStepValidatorAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
                return await Task.FromResult(true);
            return false;
        }

        //Ask for asset name if missing and store asset type
        private async Task<DialogTurnResult> AssetNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            tradeDetails.AssetType = ((FoundChoice)stepContext.Result).Value;

            if (tradeDetails.AssetName == null)
            {
                return await stepContext.PromptAsync("AssetNameStep", new PromptOptions { 
                    Prompt = MessageFactory.Text($"What {tradeDetails.AssetType} would you like to {tradeDetails.TradeIntent}?"),
                    RetryPrompt = MessageFactory.Text($"Did not find the {tradeDetails.AssetType} that you asked for. Please repeat the accurate"),
                }, cancellationToken);
            }
            return await stepContext.NextAsync(tradeDetails.AssetName, cancellationToken);
        }

        private async Task<bool> AssetNameStepValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            if (CompanyNameHelpers.Matcher(promptContext.Recognized.Value) != "")
                return await Task.FromResult(true);
            return false;
        }

        //Ask for order price if missing and store asset name - give choices to pick order type
        private async Task<DialogTurnResult> PriceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            tradeDetails.AssetName = (string)stepContext.Result;
            if (tradeDetails.Price == null)
            {

                return await stepContext.PromptAsync("PriceConfirmation",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"At what price would you like to {tradeDetails.TradeIntent} {tradeDetails.AssetName}?"),
                    RetryPrompt=MessageFactory.Text($"Please choose from the below options."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Market price", "Limit order" }),
                }, cancellationToken);
            }

            return await stepContext.NextAsync(tradeDetails.Price, cancellationToken);
        }

        //Validate whether Market price or limit order was recieved as answer
        private async Task<bool> PriceConfirmationStepValidatorAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
                return await Task.FromResult(true);
            return false;
        }

        //Ask for limit price if missing, otherwise store Market price
        private async Task<DialogTurnResult> PriceConfirmationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            if (tradeDetails.Price == null)
            {
                if (((FoundChoice)stepContext.Result).Value == "Market price")
                    tradeDetails.Price = (string)stepContext.Result;
                else
                {
                    tradeDetails.Price = null;
                    var promptMessage = MessageFactory.Text("Please type in the Limit order price at which the trade should be executed:", InputHints.ExpectingInput);
                    return await stepContext.PromptAsync("LimitPriceConfirmationStep", new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
            }
            return await stepContext.NextAsync(tradeDetails.Price, cancellationToken);
        }

        //Ask for the number of assets
        private async Task<DialogTurnResult> NumberConfrimationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            tradeDetails.Price = (string)stepContext.Result;
            if (tradeDetails.NumberOfAssets == null)
            {
                var promptMessage = MessageFactory.Text($"How many shares of {tradeDetails.AssetName} would you like to {tradeDetails.TradeIntent}?", InputHints.ExpectingInput);
                return await stepContext.PromptAsync("NumberConfrimationStep", new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(tradeDetails.NumberOfAssets, cancellationToken);
        }

        //Confirm if the input is a number and is a valid one
        private async Task<bool> NumberConfrimationStepValidatorAsync(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
            //Filter outlier values that are unlikely to be valid
                if (promptContext.Recognized.Value <= 0)
                {
                    await promptContext.Context.SendActivityAsync("Please enter a whole number greater than 0.");
                    return await Task.FromResult(false);
                }
                else if (promptContext.Recognized.Value > 500)
                {
                    await promptContext.Context.SendActivityAsync("Apologies, transactions through the chat are limited to 500 shares per trade. Please enter a value ");
                    return await Task.FromResult(false);
                }
                else
                    return await Task.FromResult(true);
            }
            await promptContext.Context.SendActivityAsync("Sorry, we did not catch that. Please reenter a numeric value.");
            return await Task.FromResult(false);
        }

        //Summarize information & ask customer to confirm inputs for the transaction
        private async Task<DialogTurnResult> ConfirmationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            tradeDetails.NumberOfAssets = (string)stepContext.Result; //purposely storing as string
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions {
                Prompt = MessageFactory.Text($"Please confirm that the following transaction details are correct:\nAsset: {tradeDetails.AssetName}\nPrice: {tradeDetails.Price}\nQuantity: {tradeDetails.NumberOfAssets}"), //turn into adaptive card
                RetryPrompt = MessageFactory.Text("Please select from the below options")
            }, cancellationToken);
        }

        //Validate if answer as yes or no
        private async Task<bool> ConfrimationStepValidatorAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
                return await Task.FromResult(true);
            return false;
        }

        //If information is correct, return to main dialog
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result) // implement logic later for the possibility of incorrect result
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your order will be executed momentarily"), cancellationToken);
            }
            return await stepContext.EndDialogAsync(null, cancellationToken);

        }
    }
}
