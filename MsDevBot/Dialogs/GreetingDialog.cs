using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MsDevBot.Models.Pizza;

namespace MsDevBot.Dialogs
{
    [Serializable]
    public class GreetingDialog : IDialog<object>
    {
        private readonly MsDevDemoDialog _msDevDemoDialog;

        public GreetingDialog()
        {
            _msDevDemoDialog = new MsDevDemoDialog();
        }
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.FromResult(0);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            if (!context.PrivateConversationData.TryGetValue("userNameIsSet", out bool _))
            {
                var sb = new StringBuilder();
                sb.AppendLine("What is your name?");
                await context.PostAsync(sb.ToString());
                context.PrivateConversationData.SetValue("userNameIsSet",true);
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                if (!context.PrivateConversationData.TryGetValue("username", out string userName))
                {
                    userName = message.Text.Trim();
                    context.PrivateConversationData.SetValue("username", userName);
                }
                await context.PostAsync($"Hello {userName}! What can I do for you?");
                context.Call(_msDevDemoDialog, OnDemoDialogDone);
            }
        }

        private async Task OnDemoDialogDone(IDialogContext context, IAwaitable<object> result)
        {
            var obj = await result ;
            if (obj is IMessageActivity activity)
            {
                await context.PostAsync(activity);
            }
            else if (obj is PizzaOrder)
            {
                await context.PostAsync("Your pizza order: " + (PizzaOrder) obj);
            }

            context.PrivateConversationData.TryGetValue("username", out string username);
            PromptDialog.Confirm(context,HelpAgainAsync, $"{username}, can I do anything else for you today?", "Sorry, I did not get that. Can I do anything else for you today?");
        }

        private async Task HelpAgainAsync(IDialogContext context, IAwaitable<bool> result)
        {
            var confirm = await result;
            if (confirm)
            {
                context.PrivateConversationData.TryGetValue("username", out string username);
                await context.PostAsync($"What can I do for you, {username}?");
                context.Call(_msDevDemoDialog, OnDemoDialogDone);
            }
            else
            {
                await context.PostAsync("Thank you for using the SuperBot! I am staying here in case you need me again");
                context.Wait(MessageReceivedAsync);
            }
        }
    }
}