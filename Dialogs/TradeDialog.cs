using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using RobinhoodBot.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RobinhoodBot.Dialogs
{
    public class TradeDialog : CancelAndHelpDialog
    {
        private const string tradeIntent = "sell";
        public TradeDialog()
        : base(nameof(TradeDialog))
            //Initialize dialogs
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AssetTypeStepAsync,
                AssetNameStepAsync,
                PriceStepAsync,
                PriceConfirmationStepAsync,
                NumberConfrimationStepAsync,
                ConfirmationStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(TradeDialog);
        }

        //Ask for Asset type if missing
        private async Task<DialogTurnResult> AssetTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tradeDetails = (TradeDetails)stepContext.Options;

            if (tradeDetails.AssetType == null)
            {
                var promptMessage = MessageFactory.Text($"What type of assets do you wish to {tradeIntent}?", InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(tradeDetails.AssetType, cancellationToken);

        }

        //Ask for asset name if missing and store asset type
        private async Task<DialogTurnResult> AssetNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tradeDetails = (TradeDetails)stepContext.Options;
            tradeDetails.AssetType = (string)stepContext.Result;

            if (tradeDetails.AssetName == null)
            {
                var promptMessage = MessageFactory.Text($"What {tradeDetails.AssetType} would you like to {tradeIntent}?", InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(tradeDetails.AssetName, cancellationToken);
        }

        //Ask for order price if missing and store asset name - give cards to pick order type
        private async Task<DialogTurnResult> PriceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tradeDetails = (TradeDetails)stepContext.Options;
            tradeDetails.AssetName = (string)stepContext.Result;
            if (tradeDetails.Price == null)
            {
                var promptMessage = MessageFactory.Text($"At what price would you like to sell {tradeDetails.AssetName}");
                promptMessage.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                    {
                        new CardAction() { Title = "Market price", Type = ActionTypes.ImBack, Value = "Market price"},
                        new CardAction() { Title = "Limit order", Type = ActionTypes.ImBack, Value = "Limit order"},
                    },
                };
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(tradeDetails.Price, cancellationToken);
        }

        //Ask for limit price if missing, otherwise store Market price

        private async Task<DialogTurnResult> PriceConfirmationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tradeDetails = (TradeDetails)stepContext.Options;
            if (tradeDetails.Price == null)
            {
                if ((string)stepContext.Result == "Market price")
                    tradeDetails.Price = (string)stepContext.Result;
                else
                {
                    tradeDetails.Price = null;
                    var promptMessage = MessageFactory.Text("Please type in the Limit order price at which the trade should be executed:", InputHints.ExpectingInput);
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
            }
            return await stepContext.NextAsync(tradeDetails.Price, cancellationToken);
        }

        //Ask for limit price if missing, otherwise store Market price
        private async Task<DialogTurnResult> NumberConfrimationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tradeDetails = (TradeDetails)stepContext.Options;
            tradeDetails.Price = (string)stepContext.Result;
            if (tradeDetails.NumberOfAssets == null)
            {
                var promptMessage = MessageFactory.Text($"How many shares of {tradeDetails.AssetName} would you like to {tradeIntent}?", InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(tradeDetails.NumberOfAssets, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tradeDetails = (TradeDetails)stepContext.Options;
            tradeDetails.NumberOfAssets = (string)stepContext.Result;
            var messageText = @$"Please confirm that the following transaction details are correct:
            Asset: {tradeDetails.AssetName}
            Price: {tradeDetails.Price}
            Quantity: {tradeDetails.NumberOfAssets}";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            promptMessage.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                    {
                        new CardAction() { Title = "Yes", Type = ActionTypes.ImBack, Value = "Yes"},
                        new CardAction() { Title = "No", Type = ActionTypes.ImBack, Value = "No"},
                    },

            };
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((string) stepContext.Result == "Yes")
            {
                var promptMessage = MessageFactory.Text("Your order will be executed momentarily");
                await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
            else
            {
                var promptMessage = MessageFactory.Text("Tough luck");
                await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
            return await stepContext.EndDialogAsync(null, cancellationToken);

        }
    }
}
