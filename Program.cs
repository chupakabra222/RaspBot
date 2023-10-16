using System;
using AngleSharp;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using AngleSharp.Dom;
using System.Text;
using System.Linq;

namespace RaspBot
{
    internal class Program
    {
        static Program()
        {
            context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
            using (StreamReader reader = new StreamReader("token.txt"))
            {
                token = reader.ReadToEnd();
            } 
            client = new TelegramBotClient(token);
            week = 1;
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
            try
            {
                ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(new[] {
                            new KeyboardButton[] {"Понедельник", "Вторник", "Среда"},
                            new KeyboardButton[] {"Четверг", "Пятница", "Суббота"},
            });


                if (update != null)
                    if (update.Message != null)
                    {


                        if (update.Message.Text == "/start")
                        {

                            await botClient.SendTextMessageAsync(
                                chatId: update.Message.Chat.Id,
                                text: "Выберите день недели",
                                replyMarkup: keyboard);
                        }

                        else
                        {
                            await HandleRequest(update, keyboard, week);
                        }
                    }
            }
            catch (Exception ex)
            {
               
            }

        }
        static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            
        }

        static async Task HandleRequest(Update update, ReplyKeyboardMarkup keyboard, int week)
        {
            int counter = 1;
            IDocument doc;

            foreach (var keyboardLine in keyboard.Keyboard)
                foreach (var btn in keyboardLine)
                {
                    if (btn.Text == update.Message.Text)
                    {
                        doc = await Parser.GetCurrentDocument(week);
                        if (doc == null)
                        {
                            await client.SendTextMessageAsync(
                                chatId: update.Message.Chat.Id,
                                text: "Расписание на эту неделю не доступно, попробуйте позже",
                                replyMarkup: keyboard);
                        }
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
                }
        }

        static IBrowsingContext context;
        static TelegramBotClient client;
        static string token;
        static int week;
        private class Parser
        {
            public static async Task<IDocument> GetCurrentDocument(int week)
            {
                string u = "https://ssau.ru/rasp";
                UriBuilder builder = new UriBuilder(u);
                builder.Query = $"groupId=755921402&selectedWeek={week.ToString()}&selectedWeekday=1";
                string uri = builder.ToString();
                IDocument doc = await context.OpenAsync(uri);

                if (doc != null)
                {
                    DateTime strDate = ParseToDate(doc);

                    if (DateTime.Now - strDate > new TimeSpan(1, 0, 0))
                        doc = await GetCurrentDocument(++week);
                }
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
                            IElement border = cells[day].QuerySelector(".lesson-border");
                            string lessonType = null;
                            if (border != null)
                                lessonType = border.ClassList[2];

                            if (lessonType != null && cells[day].TextContent.Length > 0)
                                switch (lessonType)
                                {
                                    case "lesson-border-type-1":
                                        {

                                            builder.Append("📚");
                                            break;
                                        }
                                    case "lesson-border-type-3":
                                        {
                                            builder.Append("📝");
                                            break;
                                        }
                                    case "lesson-border-type-2":
                                        {
                                            builder.Append("🧪");
                                            break;
                                        }
                                    case "lesson-border-type-4":
                                        {
                                            break;
                                        }
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