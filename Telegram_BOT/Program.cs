using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json.Linq;

namespace WeatherBot
{
    class Program
    {
        private static ITelegramBotClient botClient;

        static async Task Main(string[] args)
        {
            botClient = new TelegramBotClient("7786718127:AAFACyNIF-1pHRNJnAePzIKctI-n8o6duA0");

            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Hello, I am user {me.Id} and my name is {me.FirstName}.");

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } 
            };

            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token
            );

            Console.WriteLine("Bot is up and running. Press any key to exit.");
            Console.ReadKey();

            cts.Cancel();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
            {
                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text;

                string response = messageText switch
                {
                    "/start" => "Welcome! I am your weather bot.",
                    "/help" => "Available commands:\n/start - Welcome message\n/help - List of commands\n/weather <city> - Get weather information",
                    _ => await HandleCustomCommands(messageText)
                };

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: response,
                    cancellationToken: cancellationToken
                );
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private static async Task<string> HandleCustomCommands(string message)
        {
            if (message.StartsWith("/weather"))
            {
                var city = message.Substring(9).Trim();
                if (string.IsNullOrWhiteSpace(city))
                {
                    return "Usage: /weather <city>";
                }

                var weatherInfo = await GetWeatherAsync(city);
                return weatherInfo ?? "Could not retrieve weather information.";
            }
            else
            {
                return "Unknown command. Type /help to see available commands.";
            }
        }

        private static async Task<string> GetWeatherAsync(string city)
        {
            using var httpClient = new HttpClient();
            var apiKey = "YOUR_OPENWEATHERMAP_API_KEY";
            var url = $"http://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric";

            var response = await httpClient.GetStringAsync(url);
            var weatherData = JObject.Parse(response);

            var temp = weatherData["main"]["temp"].ToString();
            var description = weatherData["weather"][0]["description"].ToString();

            return $"Current weather in {city}:\nTemperature: {temp}°C\nDescription: {description}";
        }
    }
}
