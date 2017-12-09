using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot
{
    public class Controller
    {
        public Controller()
        {
            bot = new TelegramBotClient(Configuration.Instance.GetApiKey());
            relayController = new RelayController();
            stats = new Stats();
            lastSpeakerHeartbeat = new DateTime(2000, 1, 1);
            authorizer = new Authorizer();
            bot.OnMessage += HandleMessage;
            bot.OnCallbackQuery += HandleCallbackQuery;
            bot.OnReceiveGeneralError += (sender, e) => HandleError(e.Exception.ToString());
            bot.OnReceiveError += (sender, e) => HandleError(e.ApiRequestException.ToString());
        }

        public void Start()
        {
            bot.StartReceiving();
            var udpClient = new UdpClient(12345);
            Observable.FromAsync(udpClient.ReceiveAsync).Repeat().ObserveOn(SynchronizationContext.Current)
                      .Subscribe(_ => {
                if(lastSpeakerHeartbeat < DateTime.Now)
                {
                   lastSpeakerHeartbeat = DateTime.Now;
                }
                RefreshSpeakerState();
            });
            Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOn(SynchronizationContext.Current)
                      .Subscribe(_ => RefreshSpeakerState());
            Observable.Interval(TimeSpan.FromMinutes(2)).ObserveOn(SynchronizationContext.Current)
                      .Subscribe(_ => WriteTemperatureToDatabase());
        }

        private void HandleError(string error)
        {
            CircularLogger.Instance.Log("Bot error: {0}.", error);
            Thread.Sleep(1000);
        }

        private async void HandleMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            stats.IncrementMessageCounter();
            var userId = e.Message.From.Id;
            var chatId = e.Message.Chat.Id;
            if(!authorizer.IsAuthorized(userId))
            {
                bot.SendTextMessageAsync(chatId, "Brak dostępu.").Wait();
                CircularLogger.Instance.Log($"Unauthorized access from {GetSender(e.Message.From)}.");
                return;
            }

            if(e.Message.Text != null)
            {
                if(e.Message.Text.ToLower() == "lista")
                {
                    if(!Configuration.Instance.IsAdmin(userId))
                    {
                        bot.SendTextMessageAsync(chatId, "Tylko administrator może takie rzeczy.").Wait();
                        CircularLogger.Instance.Log($"Unauthorized listing from {GetSender(e.Message.From)}.");
                        return;
                    }
                    var users = authorizer.ListUsers();
                    foreach(var user in users.Concat(Configuration.Instance.ListAdmins()))
                    {
                        var isAdmin = Configuration.Instance.IsAdmin(user);
                        var photos = bot.GetUserProfilePhotosAsync(user).Result;
                        if(photos.TotalCount < 1)
                        {
                            continue;
                        }
                        var photo = photos.Photos[0][0];
                        var memoryStream = new MemoryStream();
                        await bot.GetFileAsync(photo.FileId, memoryStream);

                        var photoToSend = new Telegram.Bot.Types.FileToSend(photo.FileId, memoryStream);
                        var removeButton = new Telegram.Bot.Types.InlineKeyboardButton("Usuń") { CallbackData = "r" + user };
                        var markup = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[] { removeButton });
                        await bot.SendPhotoAsync(chatId, photoToSend, isAdmin ? "Administrator" : "Użytkownik",
                                                 replyMarkup: isAdmin ? null: markup);
                    }
                    return;
                }

                if (e.Message.Text.ToLower() == "restart")
                {
                    if (!Configuration.Instance.IsAdmin(userId))
                    {
                        bot.SendTextMessageAsync(chatId, "Tylko administrator może takie rzeczy.").Wait();
                        CircularLogger.Instance.Log($"Unauthorized listing from {GetSender(e.Message.From)}.");
                        return;
                    }
                    var restartMessage = bot.SendTextMessageAsync(chatId, "Wkrótce restart.").Result;
                    new Task(async () =>
                    {
                        const int noOfSeconds = 7;
                        for (var i = 0; i < noOfSeconds; i++)
                        {
                            var text = string.Format("Pozostało {0} sekund.", noOfSeconds - i);
                            bot.EditMessageTextAsync(chatId, restartMessage.MessageId, text).Wait();
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }
                        bot.EditMessageTextAsync(chatId, restartMessage.MessageId, "Teraz restart").Wait();
                        Environment.Exit(0);
                    }).Start();

                    return;
                }

                if(e.Message.Text.ToLower() == "wykres7")
                {
                    CreateChart(TimeSpan.FromDays(7), chatId, "ddd HH:mm");
                    return;
                }

                if(e.Message.Text.ToLower() == "wykres24")
                {
                    CreateChart(TimeSpan.FromDays(1), chatId, "HH:mm");
                    return;
                }

                if (e.Message.Text.ToLower() == "wykres1")
                {
                    CreateChart(TimeSpan.FromHours(1), chatId, "HH:mm");
                    return;
                }

                if (e.Message.Text.ToLower() == "historia")
                {
                    var samples = TemperatureDatabase.Instance.GetSamples(DateTime.Now - TimeSpan.FromHours(1), DateTime.Now);
                    var text = samples.Select(x => "`" + x.ToString() + "`").Aggregate((x, y) => x + Environment.NewLine + y);
                    bot.SendTextMessageAsync(chatId, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown).Wait();
                    return; 
                }

                if (e.Message.Text.ToLower() == "eksport")
                {
                    var progressMessage = bot.SendTextMessageAsync(chatId, "Przygotowuję...").Result;
                    var exportFile = TemperatureDatabase.Instance.GetSampleExport(progress =>
                    {
                        bot.EditMessageTextAsync(chatId, progressMessage.MessageId, string.Format("Wykonuję ({0:0}%)...", 100*progress)).Wait();
                    });
                    bot.EditMessageTextAsync(chatId, progressMessage.MessageId, "Wysyłam...").Wait();
                    var fileToSend = new Telegram.Bot.Types.FileToSend("probki.json.gz", File.OpenRead(exportFile));
                    bot.SendDocumentAsync(chatId, fileToSend).Wait();
                    bot.EditMessageTextAsync(chatId, progressMessage.MessageId, "Gotowe").Wait();
                    return;
                }

                if (e.Message.Text.ToLower() == "log")
                {
                    var text = CircularLogger.Instance.GetEntriesAsAString();
                    bot.SendTextMessageAsync(chatId, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown).Wait();
                    return;
                }

                var result = await HandleTextCommand(e.Message);
                bot.SendTextMessageAsync(chatId, result).Wait();
                return;
            }

            if(e.Message.Contact != null)
            {
                if(!Configuration.Instance.IsAdmin(userId))
                {
                    bot.SendTextMessageAsync(chatId, "Tylko administrator może dodawać użytownkików.").Wait();
                    CircularLogger.Instance.Log($"Trying to add remove/user from {GetSender(e.Message.From)}.");
                    return;
                }
                var contactUserId = e.Message.Contact.UserId;
                var yesButton = new Telegram.Bot.Types.InlineKeyboardButton("Tak") { CallbackData = "a" + contactUserId };
                var noButton = new Telegram.Bot.Types.InlineKeyboardButton("Przeciwnie, chcę go usunąć") { CallbackData = "r" + contactUserId };
                var keyboardMarkup = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup
                                                 (new[] { yesButton, noButton });

                bot.SendTextMessageAsync(chatId, "Autoryzować gada?", replyMarkup: keyboardMarkup).Wait();
                return;
            }

            CircularLogger.Instance.Log("Unexpected (no-text) message from {0}.", GetSender(e.Message.From));
            return;
        }

        private void CreateChart(TimeSpan timeBack, long chatId, string dateTimeFormat)
        {
            var messageToEdit = bot.SendTextMessageAsync(chatId, "Wykonuję...").Result;
            var charter = new Charter(dateTimeFormat);
            var pngFile = charter.PrepareChart(DateTime.Now - timeBack, DateTime.Now,
                                               step =>
                                               {
                                                   switch (step)
                                                   {
                                                       case Step.RetrievingData:
                                                           bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Pobieranie danych...").Wait();
                                                           break;
                                                       case Step.CreatingPlot:
                                                           bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Tworzenie wykresu...").Wait();
                                                           break;
                                                       case Step.RenderingImage:
                                                           bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Renderowanie obrazu...").Wait();
                                                           break;

                                                   }
            }, x => bot.SendTextMessageAsync(chatId, string.Format("Liczba próbek: {0}", x)));

            var fileToSend = new Telegram.Bot.Types.FileToSend("wykres", File.OpenRead(pngFile));
            bot.SendPhotoAsync(chatId, fileToSend).Wait();
            bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Gotowe.").Wait();

        }

        private void HandleCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            stats.IncrementMessageCounter();
            if(!Configuration.Instance.IsAdmin(e.CallbackQuery.From.Id))
            {
                bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "Tylko administrator może takie rzeczy.").Wait();
                CircularLogger.Instance.Log($"Trying to remove user by {GetSender(e.CallbackQuery.From)}.");
                return;
            }
            var empty = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new Telegram.Bot.Types.InlineKeyboardButton[0]);
            bot.EditMessageReplyMarkupAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId, empty).Wait();
            var data = e.CallbackQuery.Data;
            var contactId = int.Parse(data.Substring(1));
            string operation;
            if(data[0] == 'a')
            {
                operation = "Dodano";
                authorizer.AddUser(contactId);
            }
            else
            {
                operation = "Usunięto";
                authorizer.RemoveUser(contactId);
            }
            bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id,
                                     $"{operation} gada. Teraz jest ich {authorizer.ListUsers().Count()}.").Wait();
            bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "Użyj komendy 'lista', aby obejrzeć kto to jest.").Wait();
        }

        private async Task<string> HandleTextCommand(Telegram.Bot.Types.Message message)
        {
            var text = message.Text.ToLower();
            var chatId = message.Chat.Id;
            if(text.Length == 1 && char.IsDigit(text[0]))
            {
                return HandleScenario(int.Parse(text));
            }

            if(text == "staty" || text == "statystyki")
            {
                return stats.GetStats();
            }

            if(text == "ping")
            {
                return "pong";
            }

            if(text == "czuwanie")
            {
                lastSpeakerHeartbeat = (lastSpeakerHeartbeat < DateTime.Now ? DateTime.Now : lastSpeakerHeartbeat)
                    + TimeSpan.FromHours(1);
                return string.Format("Głośniki wyłączą się nie wcześniej niż o {0:HH:mm} (za {1:#.##}h).",
                                     lastSpeakerHeartbeat, (lastSpeakerHeartbeat - DateTime.Now).TotalHours);
            }

            if(text == "antyczuwanie")
            {
				lastSpeakerHeartbeat -= TimeSpan.FromHours(1);
                return string.Format("Głośniki wyłączą się nie wcześniej niż o {0:HH:mm} (za {1:#.##}h).",
									 lastSpeakerHeartbeat, (lastSpeakerHeartbeat - DateTime.Now).TotalHours);
            }

            if(text == "czas")
            {
                return DateTime.Now.ToString();
            }

            if(text == "miganie" || text == "alarm")
            {
                var random = new Random();
                var state = relayController.GetStateArray();
                for (var i = 0; i < 10; i++)
                {
                    relayController.SetState(1, true);
                    await Task.Delay(TimeSpan.FromMilliseconds(40 * random.NextDouble()));
                    relayController.SetState(2, true);
                    await Task.Delay(TimeSpan.FromMilliseconds(400 * random.NextDouble()));
                    relayController.SetState(1, false);
                    await Task.Delay(TimeSpan.FromMilliseconds(40 * random.NextDouble()));
					relayController.SetState(2, false);
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * random.NextDouble()));
                }
                relayController.SetStateFromArray(state);
                return "Wykonano.";
            }

            if(text == "bitcoin")
            {
                const string bitcoinFile = "bitcoin.txt";
                if(!File.Exists(bitcoinFile))
                {
                    return "Brak pliku z wielkością portfela.";
                }
                var data = "https://bitmarket24.pl/api/BTC_PLN/status.json".GetJsonAsync().Result;
                var value = decimal.Parse(data.last) * decimal.Parse(File.ReadAllText(bitcoinFile));
                return string.Format("Aktualna wartość: {0:0.00} PLN.", value);
            }

            if(text == "temperatura" || text == "temp")
            {
                string rawData;
                decimal temperature;
                if (!TryGetTemperature(out temperature, out rawData))
                {
                    return string.Format("Błąd CRC, przekazuję gołe dane:{0}{1}", Environment.NewLine, rawData);
                }
                return string.Format("Temperatura wynosi {0:##.#}°C.", temperature);
            }

            CircularLogger.Instance.Log($"Unknown text command '{text}'.");
            return "Nieznana komenda.";
        }

        private static bool TryGetTemperature(out decimal temperature, out string rawData)
        {
            rawData = File.ReadAllText("/sys/bus/w1/devices/28-000008e3442c/w1_slave");
            var lines = rawData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var crcMatch = new Regex(@"crc=.. (?<yesorno>\w+)").Match(lines[0]);
            if (crcMatch.Groups["yesorno"].Value != "YES")
            {
                temperature = default(decimal);
                return false;
            }
            var temperatureMatch = new Regex(@"t=(?<temperature>\d+)").Match(lines[1]);
            temperature = decimal.Parse(temperatureMatch.Groups["temperature"].Value) / 1000;
            return true;
        }

        private string HandleScenario(int scenarioNo)
        {
            if(Scenarios.Length <= scenarioNo)
            {
                return "Nie ma takiego scenariusza.";
            }
            for(var i = 0; i < RelayController.RelayCount - 1; i++)
            {
                relayController.SetState(i, Scenarios[scenarioNo].Contains(i));
            }
            return string.Format("Scenariusz {0} uaktywniony ({1}).", scenarioNo, GetLampsFriendlyName(Scenarios[scenarioNo]));
        }

        private void RefreshSpeakerState()
        {
            relayController.SetState(3, DateTime.Now - lastSpeakerHeartbeat < HeartbeatTimeout);
        }

        private void WriteTemperatureToDatabase()
        {
            string rawData;
            decimal temperature;
            if (!TryGetTemperature(out temperature, out rawData))
            {
                CircularLogger.Instance.Log("Error during adding new temperature sample to DB. Raw data: {1}{0}.", rawData, Environment.NewLine);
                return;
            }
            var database = TemperatureDatabase.Instance;
            database.AddSample(new TemperatureSample {Date = DateTime.Now, Temperature = temperature});
        }

        private static string GetSender(Telegram.Bot.Types.User user)
        {
            return $"{user.FirstName} {user.LastName}";
        }

        private static string GetLampsFriendlyName(HashSet<int> whatIsTurnedOn)
        {
            if(whatIsTurnedOn.Count == 0)
            {
                return "brak lamp";
            }
            return whatIsTurnedOn.Select(x => FriendlyNames[x]).Aggregate((x, y) => x + ", " + y);
        }

        private DateTime lastSpeakerHeartbeat;
        private readonly TelegramBotClient bot;
        private readonly RelayController relayController;
        private readonly Authorizer authorizer;
        private readonly Stats stats;

        private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(20);

        private static readonly HashSet<int>[] Scenarios = new HashSet<int>[]
        {
                new HashSet<int>(),
                new HashSet<int> { 0, 1 },
                new HashSet<int> { 1, 2 },
                new HashSet<int> { 2 },
                new HashSet<int> { 1 },
                new HashSet<int> { 0 },
                new HashSet<int> { 0, 1, 2},
                new HashSet<int> { 0, 2}
        };

        private static readonly Dictionary<int, string> FriendlyNames = new Dictionary<int, string>
        {
            { 0, "doniczka" },
            { 1, "lampa przy regale" },
            { 2, "lampa przy kanapie" }
        };
    }
}
