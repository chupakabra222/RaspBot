using AngleSharp;
using AngleSharp.Dom;
using Microsoft.VisualBasic;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace RaspBot
{
    internal class Program
    {
        static Program()
        {
            context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
            token = "6591554925:AAFQjEOLUACFTtpkgF0g6RhPGch17NHy3wY";
            client = new TelegramBotClient(token);
        }
        static async Task Main(string[] args)
        {
            CancellationTokenSource src = new CancellationTokenSource();
            client.StartReceiving(
                updateHandler: TgUpdateHandler,
                pollingErrorHandler: HandleErrorAsync,
                cancellationToken: src.Token);
            await Task.Delay(-1);

        }
        static async Task TgUpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(new[] {
                            new KeyboardButton[] {"Понедельник", "Вторник", "Среда"},
                            new KeyboardButton[] {"Четверг", "Пятница", "Суббота"}}); ;
            if (update != null)
                if (update.Message != null)
                    if (update.Message.Text == "/start")
                    {

                        await botClient.SendTextMessageAsync(
                            chatId: update.Message.Chat.Id,
                            text: "Выберите день недели",
                            replyMarkup: keyboard);
                    }
                    else
                    {
                        int counter = 1;
                        foreach (var keyboardLine in keyboard.Keyboard)
                            foreach (var btn in keyboardLine)
                            { 
                                if (btn.Text == update.Message.Text)
                                {
                                    IDocument doc = await Parser.GetCurrentDocument(1);
                                    string rasp = Parser.GetRasp(doc, counter);
                                    if (rasp != null)
                                    {
                                        await client.SendTextMessageAsync(
                                            chatId: update.Message.Chat.Id,
                                            text: rasp,
                                            replyMarkup: keyboard);
                                    }
                                    else
                                    {
                                        await client.SendTextMessageAsync(
                                            chatId: update.Message.Chat.Id,
                                            text: "Ошибка",
                                            replyMarkup: keyboard);
                                    }
                                }
                                counter++;
                    }       }


        }
        static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception.Message);
        }

        static IBrowsingContext context;
        static TelegramBotClient client;
        static string token;
        private class Parser
        {
            public static async Task<IDocument> GetCurrentDocument(int week)
            {
                string u = "https://ssau.ru/rasp";
                UriBuilder builder = new UriBuilder(u);
                builder.Query = $"groupId=755921402&selectedWeek={week.ToString()}&selectedWeekday=1";
                string uri = builder.ToString();
                IDocument doc = await context.OpenAsync(uri);

                DateTime strDate = ParseToDate(doc);

                if (DateTime.Now - strDate > new TimeSpan(1, 0, 0))
                    doc = await GetCurrentDocument(++week);
                return doc;
            }
            public static DateTime ParseToDate(IDocument doc)
            {
                IElement table = doc.QuerySelector(".schedule__items");
                IHtmlCollection<IElement> dayColl = table.QuerySelectorAll(".schedule__head-date");
                string[] strArray = dayColl.Last().TextContent.Split(".");
                DateTime time = new DateTime(Int32.Parse(strArray[2]), Int32.Parse(strArray[1]), Int32.Parse(strArray[0]));
                return time;
            }
            public static string GetRasp(IDocument page, int day)
            {
                IElement table = page.QuerySelector(".schedule__items");
                IHtmlCollection<IElement> cells = table.QuerySelectorAll(".schedule__item");
                IHtmlCollection<IElement> timeCells = table.QuerySelectorAll(".schedule__time");
                StringBuilder builder = new StringBuilder();

                int counter = day;
                int timeSelector = 0;
                foreach (IElement cell in cells)
                {
                    if (counter % 6 == 0)
                    {
                        if (day < cells.Count() - 1)
                        {
                            if (counter != 6)
                            {
                                IHtmlCollection<IElement> time = timeCells[timeSelector].QuerySelectorAll(".schedule__time-item");
                                builder.Append(time[0].TextContent);
                                builder.Append("-");
                                builder.Append(time[1].TextContent);
                                timeSelector++;
                            }
                            builder.Append(cells[day].TextContent);
                        }
                        builder.AppendLine();
                        day += 6;

                    }
                    counter++;
                }

                return builder.ToString();
            }
        }
    }

}