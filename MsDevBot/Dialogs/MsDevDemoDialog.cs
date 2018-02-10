using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using MsDevBot.Models.Pizza;
using MsDevBot.Models.Weather;

namespace MsDevBot.Dialogs
{
    [LuisModel("{app_id}", "{subscription_id}")]
    [Serializable]
    public class MsDevDemoDialog : LuisDialog<object>
    {
        private readonly WeatherDataProcessor _weatherDataProcessor;
        [NonSerialized]
        private IMessageActivity _message;

        public MsDevDemoDialog()
        {
            _weatherDataProcessor = new WeatherDataProcessor();
            
        }

        protected override async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            _message = await item;
            await base.MessageReceived(context, item);
        }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            var messageActivity = context.MakeMessage();
            messageActivity.Text = "Sorry I did not understand what you asked me. Please try again";
            context.Done(messageActivity);
        }

        [LuisIntent("GetWeather")]
        public async Task GetWeatherAtLocation(IDialogContext context, LuisResult result)
        {
            var messageActivity = context.MakeMessage();
            try
            {
                if (TryGetWeather(result, out var cityName))
                {

                    var city =
                        _weatherDataProcessor.Cities.SingleOrDefault(
                            _ => string.Equals(_.NameEN, cityName, StringComparison.InvariantCultureIgnoreCase) ||
                                 string.Equals(_.NameFR, cityName, StringComparison.InvariantCultureIgnoreCase));

                    if (!city.Equals(default(WeatherCity)))
                    {
                        var cityData = await _weatherDataProcessor.GetWeather(city);
                        var attachment = new Attachment
                        {
                            ContentUrl = $"http://weather.gc.ca/weathericons/{cityData.IconFileName}",
                            ContentType = $"image/{cityData.IconType}"
                        };

                        messageActivity.Text = $"Current conditions in {city.NameEN}: {cityData.Conditions} {cityData.Temperature}{cityData.TemperatureUnit}";
                        messageActivity.Attachments = new List<Attachment> {attachment};
                    }
                    else
                        messageActivity.Text = "Sorry I could not find the weather for the city you requested";
                }
                else
                    messageActivity.Text = "Sorry I could not find the weather for the city you requested";
            }
            catch (Exception ex)
            {
                messageActivity.Text = ex.ToString();
            }

            context.Done(messageActivity);
        }

        [LuisIntent("OrderPizza")]
        public async Task OrderPizza(IDialogContext context, LuisResult result)
        {
            await context.Forward(Chain.From(() =>new PizzaOrderDialog(BuildForm)),OnPizzaOrderingDone, _message, CancellationToken.None);
        }

        private async Task OnPizzaOrderingDone(IDialogContext context, IAwaitable<object> result)
        {
            var childResult = await result;
            context.Done(childResult);
        }

        private bool TryGetWeather(LuisResult result, out string cityName)
        {
            if (result.TryFindEntity("builtin.geography.city", out var location))
            {
                cityName = location.Entity;
                return true;
            }

            cityName = null;
            return false;
        }

        private static IForm<PizzaOrder> BuildForm()
        {
            var builder = new FormBuilder<PizzaOrder>();

            ActiveDelegate<PizzaOrder> isBYO = pizza => pizza.Kind == PizzaOptions.BYOPizza;
            ActiveDelegate<PizzaOrder> isSignature = pizza => pizza.Kind == PizzaOptions.SignaturePizza;
            ActiveDelegate<PizzaOrder> isGourmet = pizza => pizza.Kind == PizzaOptions.GourmetDelitePizza;
            ActiveDelegate<PizzaOrder> isStuffed = pizza => pizza.Kind == PizzaOptions.StuffedPizza;

            return builder
                // .Field(nameof(PizzaOrder.Choice))
                .Field(nameof(PizzaOrder.Size))
                .Field(nameof(PizzaOrder.Kind))
                .Field("BYO.Crust", isBYO)
                .Field("BYO.Sauce", isBYO)
                .Field("BYO.Toppings", isBYO)
                .Field(nameof(PizzaOrder.GourmetDelite), isGourmet)
                .Field(nameof(PizzaOrder.Signature), isSignature)
                .Field(nameof(PizzaOrder.Stuffed), isStuffed)
                .AddRemainingFields()
                .Confirm("Would you like a {Size}, {BYO.Crust} crust, {BYO.Sauce}, {BYO.Toppings} pizza?", isBYO)
                .Confirm("Would you like a {Size}, {&Signature} {Signature} pizza?", isSignature, new[] { "Size", "Kind", "Signature" })
                .Confirm("Would you like a {Size}, {&GourmetDelite} {GourmetDelite} pizza?", isGourmet)
                .Confirm("Would you like a {Size}, {&Stuffed} {Stuffed} pizza?", isStuffed)
                .Build()
                ;
        }
    }
}