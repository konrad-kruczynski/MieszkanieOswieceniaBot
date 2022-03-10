using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Humanizer;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MieszkanieOswieceniaBot
{
    public class Controller
    {
        public Controller()
        {
            CircularLogger.Instance.Log("Initializing bot...");
            bot = new TelegramBotClient(Configuration.Instance.GetApiKey());
            stats = new Stats();
            pekaClients = new Dictionary<string, PekaClient>();
            lastSpeakerHeartbeat = Enumerable.Repeat(new DateTime(2000, 1, 1).ToUniversalTime(), 2).ToArray();
            authorizer = new Authorizer();
            CircularLogger.Instance.Log("Bot initialized.");
        }

        public async Task HandleUpdate(Telegram.Bot.Types.Update update)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQuery(update.CallbackQuery);
                return;
            }

            if (update.Type != UpdateType.Message)
            {
                return;
            }

            if (update.Message!.Type != MessageType.Text)
            {
                return;
            }

            await HandleMessage(update.Message);
        }

        private void SubscribeOnInterval(TimeSpan period, SynchronizationContext context, Func<Task> handlerAsync)
        {
            Observable.Interval(period)
                .Select(x => Observable.FromAsync(handlerAsync).ObserveOn(context))
                .Concat().Subscribe();
        }

        public async Task Run()
        {
            CircularLogger.Instance.Log("Starting bot...");
            startDate = DateTime.UtcNow;
            var udpEndPoint = new IPEndPoint(IPAddress.Any, 12345);
            var udpClient = new UdpClient(udpEndPoint);

            Observable.FromAsync(udpClient.ReceiveAsync).Repeat().ObserveOn(SynchronizationContext.Current)
                .Select(x => Observable.FromAsync(async () => await HandleUdp(x)).ObserveOn(SynchronizationContext.Current))
                .Concat().Subscribe();

            SubscribeOnInterval(TimeSpan.FromSeconds(10), SynchronizationContext.Current, RefreshSpeakerState);
            SubscribeOnInterval(TimeSpan.FromMinutes(2), SynchronizationContext.Current, WriteTemperatureAndStateToDatabase);
            SubscribeOnInterval(TimeSpan.FromMinutes(1), SynchronizationContext.Current, HandleAutoScenarioTimer);
            SubscribeOnInterval(TimeSpan.FromHours(8), SynchronizationContext.Current, CheckHousingCooperativeNews);

            CircularLogger.Instance.Log("Bot started.");

            await foreach (var update in new Telegram.Bot.Extensions.Polling.QueuedUpdateReceiver(bot))
            {
                await HandleUpdate(update);
            }
        }

        private async Task HandleUdp(UdpReceiveResult result)
        {
            var bufferAsString = Encoding.UTF8.GetString(result.Buffer);
            if(int.TryParse(bufferAsString, out var number))
            {
                if(number == 10)
                {
                    await Globals.Relays[3].Relay.TrySetStateAsync(true);
                }

                if(number == 11)
                {
                    await Globals.Relays[3].Relay.TrySetStateAsync(false);
                }

                await HandleScenarioAsync(number);
                return;
            }

            if(lastSpeakerHeartbeat[0] < DateTime.UtcNow)
            {
                lastSpeakerHeartbeat[0] = DateTime.UtcNow;
            }

            await RefreshSpeakerState();
        }

        private void HandleError(string error)
        {
            CircularLogger.Instance.Log("Bot error: {0}.", error);
        }

        private async Task HandleMessage(Telegram.Bot.Types.Message message)
        {
            stats.IncrementMessageCounter();
            var userId = message.From.Id;
            var chatId = message.Chat.Id;
            if(!authorizer.IsAuthorized(userId))
            {
                await bot.SendTextMessageAsync(chatId, "Brak dostępu.");
                CircularLogger.Instance.Log($"Unauthorized access from {GetSender(message.From)}.");
                return;
            }

            if(message.Date < startDate)
            {
                await bot.SendTextMessageAsync(message.Chat.Id,
                                         string.Format("Wiadomość '{0}' została wysłana {1}, tj. przed startem bota, który nastąpił {2}. Proszę ponowić.",
                                         message.Text, message.Date, startDate));
                return;
            }

            if(message.Text != null)
            {
                if(message.Text.ToLower() == "lista")
                {
                    if(!Configuration.Instance.IsAdmin(userId))
                    {
                        await bot.SendTextMessageAsync(chatId, "Tylko administrator może takie rzeczy.");
                        CircularLogger.Instance.Log($"Unauthorized listing from {GetSender(message.From)}.");
                        return;
                    }
                    var users = authorizer.ListUsers();
                    foreach(var user in users.Concat(Configuration.Instance.ListAdmins()))
                    {
                        var isAdmin = Configuration.Instance.IsAdmin(user);
                        var photos = await bot.GetUserProfilePhotosAsync(user);
                        if(photos.TotalCount < 1)
                        {
                            continue;
                        }

                        var photo = photos.Photos[0][0];

                        var markup = new InlineKeyboardMarkup(
                            new[] { InlineKeyboardButton.WithCallbackData("Usuń", "r" + user) });
                        var photoToSend = new Telegram.Bot.Types.InputFiles.InputOnlineFile(photo.FileId.ToString());
                        await bot.SendPhotoAsync(chatId, photoToSend, isAdmin ? "Administrator" : "Użytkownik",
                                                 replyMarkup: isAdmin ? null : markup);
                    }
                    return;
                }

                if(message.Text.ToLower() == "restart")
                {
                    if(!Configuration.Instance.IsAdmin(userId))
                    {
                        await bot.SendTextMessageAsync(chatId, "Tylko administrator może takie rzeczy.");
                        CircularLogger.Instance.Log($"Unauthorized listing from {GetSender(message.From)}.");
                        return;
                    }
                    await bot.SendTextMessageAsync(chatId, "Teraz restart");
                    Environment.Exit(0);

                    return;
                }

                if(message.Text.ToLower() == "wykres7")
                {
                    await CreateChart(TimeSpan.FromDays(7), chatId, "ddd HH:mm", false);
                    return;
                }

                if(message.Text.ToLower() == "wykres24")
                {
                    await CreateChart(TimeSpan.FromDays(1), chatId, "HH:mm", false);
                    return;
                }

                if(message.Text.ToLower() == "wykres48")
                {
                    await CreateChart(TimeSpan.FromDays(2), chatId, "HH:mm", false);
                    return;
                }

                if(message.Text.ToLower() == "wykres30")
                {
                    await CreateChart(TimeSpan.FromDays(30), chatId, "dd", false);
                    return;
                }

                if (message.Text.ToLower() == "wykres48-2")
                {
                    await CreateChart(TimeSpan.FromDays(2), chatId, "HH:mm", true);
                    return;
                }

                if(message.Text.ToLower() == "wykres7-2")
                {
                    await CreateChart(TimeSpan.FromDays(7), chatId, "HH:mm", true);
                    return;
                }

                if(message.Text.ToLower() == "histogram0")
                {
                    await CreateHistogram(chatId, new[] { 0 });
                    return;
                }
                if(message.Text.ToLower() == "histogram1")
                {
                    await CreateHistogram(chatId, new[] { 1 });
                    return;
                }

                if(message.Text.ToLower() == "histogram2")
                {
                    await CreateHistogram(chatId, new[] { 2 });
                    return;
                }

                if(message.Text.ToLower() == "histogram3")
                {
                    await CreateHistogram(chatId, new[] { 3 });
                    return;
                }

                if(message.Text.ToLower() == "histogram4")
                {
                    await CreateHistogram(chatId, new[] { 4 });
                    return;
                }

                if(message.Text.ToLower() == "histogram5")
                {
                    await CreateHistogram(chatId, new[] { 5 });
                    return;
                }

                if(message.Text.ToLower() == "histogram6")
                {
                    await CreateHistogram(chatId, new[] { 6 });
                    return;
                }

                if(message.Text.ToLower() == "histogram7")
                {
                    await CreateHistogram(chatId, new[] { 7 });
                    return;
                }

                if(message.Text.ToLower() == "superhistogram")
                {
                    await CreateHistogram(chatId, new[] { 0, 1, 2, 3, 4, 5, 6 });
                    return;
                }

                if(message.Text.ToLower() == "historia")
                {
                    var samples = Database.Instance.GetNewestSamples<TemperatureSample>(30);
                    var text = samples.Select(x => "`" + x.ToString() + "`").Aggregate((x, y) => x + Environment.NewLine + y);
                    await bot.SendTextMessageAsync(chatId, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    return;
                }

                if(message.Text.ToLower() == "historia2")
                {
                    var samples = Database.Instance.GetNewestSamples<RelaySample>(20 * Globals.Relays.Count);
                    var samplesGroupedByMinutes = samples.GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, x.Date.Day, x.Date.Hour, x.Date.Minute, 0));
                    samplesGroupedByMinutes = samplesGroupedByMinutes.OrderByDescending(x => x.Key).Take(20).OrderBy(x => x.Key);
                    var resultString = new StringBuilder();
                    var maximalRelayNumber = Globals.Relays.Max(x => x.Key);
                    foreach (var group in samplesGroupedByMinutes)
                    {
                        resultString.AppendFormat("`{0:R}: ", group.Key);
                        for (var i = 0; i <= maximalRelayNumber; i++)
                        {
                            if (!Globals.Relays.ContainsKey(i))
                            {
                                resultString.Append('◌');
                            }
                            else if(group.Any(x => x.RelayId == i && x.State))
                            {
                                resultString.Append('●');
                            }
                            else
                            {
                                resultString.Append('○');
                            }
                        }

                        resultString.Append('`');
                        resultString.AppendLine();
                    }

                    var text = resultString.ToString();
                    await bot.SendTextMessageAsync(chatId, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    return;
                }

                if(message.Text.ToLower() == "eksport")
                {
                    var progressMessage = await bot.SendTextMessageAsync(chatId, "Przygotowuję...");
                    var lastMessage = string.Empty;
                    var exportFile = await Database.Instance.GetTemperatureSampleExport(async progress =>
                    {
                        var message = string.Format("Wykonuję ({0:0}%)...", 100 * progress);
                        if (message != lastMessage)
                        {
                            await bot.EditMessageTextAsync(chatId, progressMessage.MessageId, message);
                            lastMessage = message;
                        }
                    });
                    await bot.EditMessageTextAsync(chatId, progressMessage.MessageId, "Wysyłam...");
                    var fileToSend = new Telegram.Bot.Types.InputFiles.InputOnlineFile(File.OpenRead(exportFile), "probki.json.gz");
                    await bot.SendDocumentAsync(chatId, fileToSend);
                    await bot.EditMessageTextAsync(chatId, progressMessage.MessageId, "Gotowe");
                    return;
                }

                if(message.Text.ToLower() == "peka")
                {
                    foreach(var pekaEntry in PekaDb.Instance.GetData())
                    {
                        if(!pekaClients.TryGetValue(pekaEntry.Item2, out var client))
                        {
                            client = new PekaClient(pekaEntry.Item2, pekaEntry.Item3);
                            pekaClients[pekaEntry.Item2] = client;
                        }
                        var balance = await client.GetCurrentBalance();
                        await bot.SendTextMessageAsync(chatId, string.Format("{0}: {1:0.00} PLN", pekaEntry.Item1, balance));
                    }
                    return;
                }

                if(message.Text.ToLower() == "log")
                {
                    var entries = CircularLogger.Instance.GetEntriesAsHtmlStrings().ToList();
                    if (entries.Count > 10)
                    {
                        entries.RemoveRange(0, entries.Count - 10);
                    }

                    foreach (var line in entries)
                    {
                        await bot.SendTextMessageAsync(chatId, line, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                    }
                    return;
                }

                var result = await HandleTextCommand(message);
                await bot.SendTextMessageAsync(chatId, result);
                return;
            }

            if(message.Contact != null)
            {
                if(!Configuration.Instance.IsAdmin(userId))
                {
                    await bot.SendTextMessageAsync(chatId, "Tylko administrator może dodawać użytownkików.");
                    CircularLogger.Instance.Log($"Trying to add remove/user from {GetSender(message.From)}.");
                    return;
                }
                var contactUserId = message.Contact.UserId;


                var yesButton = InlineKeyboardButton.WithCallbackData("Tak", "a" + contactUserId);
                var noButton = InlineKeyboardButton.WithCallbackData("Przeciwnie, chcę go usunąć", "r");
                var keyboardMarkup = new InlineKeyboardMarkup(new[] { yesButton, noButton });

                await bot.SendTextMessageAsync(chatId, "Autoryzować gada?", replyMarkup: keyboardMarkup);
                return;
            }

            CircularLogger.Instance.Log("Unexpected (no-text) message from {0}.", GetSender(message.From));
            return;
        }

        private async Task CreateChart(TimeSpan timeBack, long chatId, string dateTimeFormat, bool oneDay)
        {
            var messageToEdit = await bot.SendTextMessageAsync(chatId, "Wykonuję...");
            var charter = new Charter(dateTimeFormat);
            var pngFile = await charter.PrepareChart(DateTime.Now - timeBack, DateTime.Now, oneDay,
                                               async step =>
                                               {
                                                   switch(step)
                                                   {
                                                       case Step.RetrievingData:
                                                           await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Pobieranie danych...");
                                                           break;
                                                       case Step.CreatingPlot:
                                                           await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Tworzenie wykresu...");
                                                           break;
                                                       case Step.RenderingImage:
                                                           await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Renderowanie obrazu...");
                                                           break;

                                                   }
                                               }, x => bot.SendTextMessageAsync(chatId, string.Format("Liczba próbek: {0}", x)));


            var fileToSend = new Telegram.Bot.Types.InputFiles.InputOnlineFile(File.OpenRead(pngFile));
            await bot.SendPhotoAsync(chatId, fileToSend);
            await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Gotowe.");

        }

        private async Task CreateHistogram(long chatId, int[] relayNos)
        {
            var names = relayNos.Select(x => Globals.Relays[x].FriendlyName);
            var chartName = names.Aggregate(string.Empty, (x, y) => x + "/" + y);
            var messageToEdit = await bot.SendTextMessageAsync(chatId, "Wykonuję..."); ;
            var charter = new Charter("");
            var pngFile = await charter.PrepareHistogram(relayNos, chartName, async step =>
                                               {
                                                   switch(step)
                                                   {
                                                       case Step.RetrievingData:
                                                           await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Pobieranie danych...");
                                                           break;
                                                       case Step.CreatingPlot:
                                                           await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Tworzenie wykresu...");
                                                           break;
                                                       case Step.RenderingImage:
                                                           await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Renderowanie obrazu...");
                                                           break;

                                                   }
                                               });

            var fileToSend = new Telegram.Bot.Types.InputFiles.InputOnlineFile(File.OpenRead(pngFile));
            await bot.SendPhotoAsync(chatId, fileToSend);
            await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Gotowe.");

        }

        private async Task HandleCallbackQuery(Telegram.Bot.Types.CallbackQuery callbackQuery)
        {
            stats.IncrementMessageCounter();
            if(!Configuration.Instance.IsAdmin(callbackQuery.From.Id))
            {
                await bot.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Tylko administrator może takie rzeczy.");
                CircularLogger.Instance.Log($"Trying to remove user by {GetSender(callbackQuery.From)}.");
                return;
            }
            var empty = new InlineKeyboardMarkup(new InlineKeyboardButton[0]);
            await bot.EditMessageReplyMarkupAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, empty);
            var data = callbackQuery.Data;
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
            await bot.SendTextMessageAsync(callbackQuery.Message.Chat.Id,
                                     $"{operation} gada. Teraz jest ich {authorizer.ListUsers().Count()}.");
            await bot.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Użyj komendy 'lista', aby obejrzeć kto to jest.");
        }

        private async Task<string> HandleTextCommand(Telegram.Bot.Types.Message message)
        {
            var text = message.Text.ToLower();
            var chatId = message.Chat.Id;
            if(text.Length == 1 && char.IsDigit(text[0]))
            {
                return await HandleScenarioAsync(int.Parse(text));
            }

            if(text == "staty" || text == "statystyki")
            {
                return stats.GetStats();
            }

            if(text == "ping")
            {
                return "pong";
            }

            if(text.StartsWith("czuwanie"))
            {
                var index = text == "czuwanie" ? 0 : 1;
                lastSpeakerHeartbeat[index] = ((lastSpeakerHeartbeat[index] < DateTime.UtcNow ? DateTime.UtcNow : lastSpeakerHeartbeat[index]))
                    + TimeSpan.FromHours(1);
                await RefreshSpeakerState();
                return string.Format("Głośniki wyłączą się nie wcześniej niż o {0:HH:mm} UTC ({1}).",
                                     lastSpeakerHeartbeat[index], (lastSpeakerHeartbeat[index] - DateTime.UtcNow).Humanize(culture: PolishCultureInfo));
            }

            if(text.StartsWith("antyczuwanie"))
            {
                var index = text == "czuwanie" ? 0 : 1;
                lastSpeakerHeartbeat[index] -= TimeSpan.FromHours(1);
                await RefreshSpeakerState();
                return string.Format("Głośniki wyłączą się nie wcześniej niż o {0:HH:mm} UTC ({1}).",
                                     lastSpeakerHeartbeat[index], (lastSpeakerHeartbeat[index] - DateTime.UtcNow).Humanize(culture: PolishCultureInfo));
            }

            if(text.StartsWith("grzanie"))
            {
                int relayNo;
                if(text == "grzanie")
                {
                    var relayNos = new[] { 4, 5 };
                    var relays = relayNos.Select(x => Globals.Relays[x].Relay);
                    var tasks = relays.Select(x => x.GetFriendlyStateAsync()).ToArray();

                    return string.Format("Kot: {0}{1}Kocica: {2}", await tasks[0], Environment.NewLine, await tasks[1]);
                }
                else if(text == "grzanie kot")
                {
                    relayNo = 4;
                }
                else if(text == "grzanie kocica")
                {
                    relayNo = 5;
                }
                else
                {
                    return "Niepoprawna informacja kogo grzać";
                }

                var result = await Globals.Relays[relayNo].Relay.TryToggleAsync();
                if (!result.Success)
                {
                    return "Nie udało się włączyć lub wyłączyć grzania. Spróbuj ponownie za jakiś czas.";
                }

                switch (result.CurrentState)
                {
                    case true:
                        return "Grzanie włączono";
                    case false:
                        return "Grzanie wyłączono";
                }
            }

            if(text == "czas")
            {
                return DateTime.Now.ToString();
            }

            if(text == "miganie" || text == "alarm")
            {
                var random = new Random();
                var originalState1 = await Globals.Relays[1].Relay.TryGetStateAsync();
                var originalState2 = await Globals.Relays[2].Relay.TryGetStateAsync();

                for(var i = 0; i < 10; i++)
                {
                    await Globals.Relays[1].Relay.TrySetStateAsync(true);
                    await Task.Delay(TimeSpan.FromMilliseconds(40 * random.NextDouble()));
                    await Globals.Relays[2].Relay.TrySetStateAsync(true);
                    await Task.Delay(TimeSpan.FromMilliseconds(400 * random.NextDouble()));
                    await Globals.Relays[1].Relay.TrySetStateAsync(false);
                    await Task.Delay(TimeSpan.FromMilliseconds(40 * random.NextDouble()));
                    await Globals.Relays[2].Relay.TrySetStateAsync(false);
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * random.NextDouble()));
                }

                await Globals.Relays[1].Relay.TrySetStateAsync(originalState1.State);
                await Globals.Relays[2].Relay.TrySetStateAsync(originalState2.State);
                return "Wykonano.";
            }

            if(text == "bitcoin")
            {
                const string currencyFile = "currency.txt";
                if(!File.Exists(currencyFile))
                {
                    return "Brak pliku z wielkością portfeli.";
                }
                var btcTask = "https://api.zonda.exchange/rest/trading/stats/BTC-PLN".GetJsonAsync();
                var ltcTask = "https://api.zonda.exchange/rest/trading/stats/LTC-PLN".GetJsonAsync();
                var btcData = await btcTask;
                var ltcData = await ltcTask;
                var currencyFileLines = File.ReadAllLines(currencyFile);
                var btcValue = decimal.Parse(btcData.stats.l) * decimal.Parse(currencyFileLines[0]);
                var originalBtcValue = decimal.Parse(currencyFileLines[1]);
                var ltcValue = decimal.Parse(ltcData.stats.l) * decimal.Parse(currencyFileLines[2]);
                var originalLtcValue = decimal.Parse(currencyFileLines[3]);
                return string.Format("Bitcoin: {0:0.00}PLN ({1:0.#}x)\nLitecoin: {2:0.00}PLN ({3:0.#}x)\nRazem: {4:0.00}PLN  ({5:0.#}x)",
                                     btcValue, btcValue / originalBtcValue, ltcValue, ltcValue / originalLtcValue,
                                     btcValue + ltcValue, (btcValue + ltcValue) / (originalBtcValue + originalLtcValue));
            }

            if(text == "temperatura" || text == "temp")
            {
                string rawData;
                decimal temperature;
                if(!TryGetTemperature(out temperature, out rawData))
                {
                    return string.Format("Błąd CRC, przekazuję gołe dane:{0}{1}", Environment.NewLine, rawData);
                }
                return string.Format("Temperatura wynosi {0:##.#}°C.", temperature);
            }

            if(text == "różany")
            {
                Database.Instance.AddHouseCooperativeChatId(message.Chat.Id);
                return "Dodano";
            }

            if(text == "z")
            {
                var result = await Globals.Relays[6].Relay.TryToggleAsync();
                if (!result.Success)
                {
                    return "Nie udało się przełączyć stanu. Spróbuj ponownie później.";
                }

                switch (result.CurrentState)
                {
                    case true:
                        return "Światło włączono";
                    case false:
                        return "Światło wyłączono";
                }
            }

            if(text == "r")
            {
                var result = await Globals.Relays[7].Relay.TryToggleAsync();
                if (!result.Success)
                {
                    return "Nie udało się przełączyć stanu. Spróbuj ponownie później.";
                }

                switch (result.CurrentState)
                {
                    case true:
                        return "Światło włączono";
                    case false:
                        return "Światło wyłączono";
                }
            }

            if(text == "reset różanego")
            {
                Database.Instance.NewestKnownRosyCreekNewsDate = DateTime.MinValue;
                return "Zresetowano";
            }

            if(text == "odpady")
            {
                var wasteEntries = new List<(string, DateTime)>();

                using(var reader = new StreamReader("odpady.csv"))
                {
                    var row = reader.ReadLine().Split(';');
                    var typesOfWaste = new string[row.Length];
                    for(var i = 0; i < typesOfWaste.Length; i++)
                    {
                        typesOfWaste[i] = row[i];
                    }

                    for(var i = 1; i <= 12; i++)
                    {
                        row = reader.ReadLine().Split(';');
                        for(var j = 0; j < typesOfWaste.Length; j++)
                        {
                            var days = row[j].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            var wasteElements = days.Select(dayNo => (typesOfWaste[j], new DateTime(DateTime.Now.Year, i, int.Parse(dayNo))));
                            wasteEntries.AddRange(wasteElements);
                        }
                    }
                }

                var wasteData = wasteEntries.GroupBy(x => x.Item1).ToDictionary(x => x.Key, x => x.Select(y => y.Item2).ToArray());

                string WasteDateToString(DateTime date)
                {
                    if (date == default)
                    {
                        return "???";
                    }

                    return String.Format("{0:ddd dd MMM}", date);
                }

                var result = new StringBuilder();
                foreach(var waste in wasteData)
                {
                    var dates = waste.Value;
                    var nearestInFuture = dates.Where(x => x > DateTime.Now).OrderBy(x => x - DateTime.Now).FirstOrDefault();
                    var nearestInPast = dates.Where(x => x <= DateTime.Now).OrderBy(x => DateTime.Now - x).FirstOrDefault();
                    result.AppendFormat("{0}: {1} <-> {2}", waste.Key, WasteDateToString(nearestInPast), WasteDateToString(nearestInFuture));
                    result.AppendLine();
                }

                return result.ToString();
            }

            if (text == "stan")
            {
                var result = new StringBuilder();
                var turnedOns = new List<string>();

                var relays = Globals.Relays.OrderBy(x => x.Key);
                var relaysWithStates = relays.Select(x => (x.Value, x.Value.Relay.TryGetStateAsync())).ToArray();

                foreach (var (relay, stateTask) in relaysWithStates)
                {
                    var state = await stateTask;
                    result.AppendLine($"{relay.Id} ({relay.FriendlyName}): {RelayExtensions.GetFriendlyStateFromSuccessAndState(state)}");
                    if (state.Success && state.State)
                    {
                        turnedOns.Add(relay.FriendlyName);
                    }
                }

                if(turnedOns.Count > 0)
                {
                    result.AppendLine();
                    result.AppendLine("Włączone:");

                    foreach(var turnedOn in turnedOns)
                    {
                        result.AppendLine(turnedOn);
                    }
                }

                return result.ToString();
            }

            CircularLogger.Instance.Log($"Unknown text command '{text}'.");
            return "Nieznana komenda.";
        }

        private static bool TryGetTemperature(out decimal temperature, out string rawData)
        {
            rawData = File.ReadAllText("/sys/bus/w1/devices/28-000008e3442c/w1_slave");
            var lines = rawData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var crcMatch = new Regex(@"crc=.. (?<yesorno>\w+)").Match(lines[0]);
            if(crcMatch.Groups["yesorno"].Value != "YES")
            {
                temperature = default(decimal);
                return false;
            }
            var temperatureMatch = new Regex(@"t=(?<temperature>\d+)").Match(lines[1]);
            temperature = decimal.Parse(temperatureMatch.Groups["temperature"].Value) / 1000;
            return true;
        }

        private async Task<string> HandleScenarioAsync(int scenarioNo)
        {
            if(Globals.Scenarios.Length <= scenarioNo)
            {
                return "Nie ma takiego scenariusza.";
            }
         
            var scenario = Globals.Scenarios[scenarioNo];
            if (!await scenario.TryApplyAsync(Globals.Relays))
            {
                return "Nie udało się w całości wykonać scenariusza";
            }

            return string.Format("Scenariusz {0} uaktywniony ({1}).", scenarioNo, scenario.GetFriendlyDescription(Globals.Relays));
        }

        private async Task HandleAutoScenarioTimer()
        {
            foreach (var autoScenario in Globals.AutoScenarios)
            {
                await autoScenario.RefreshAsync();
            }
        }

        private async Task RefreshSpeakerState()
        {
            await Globals.Relays[3].Relay.TrySetStateAsync(DateTime.UtcNow - lastSpeakerHeartbeat[0] < Globals.HeartbeatTimeout);
            await Globals.Relays[8].Relay.TrySetStateAsync(DateTime.UtcNow - lastSpeakerHeartbeat[1] < Globals.HeartbeatTimeout);
        }

        private async Task CheckHousingCooperativeNews()
        {
            var rosyCreekClient = new RosyCreekClient();
            var result = await rosyCreekClient.TryGetNewsAsync();
            if (!result.Success)
            {
                return;
            }

            var message = result.Message;
            var chatIds = Database.Instance.GetHouseCooperativeChatIds();
            foreach(var chatId in chatIds)
            {
                await bot.SendTextMessageAsync(chatId, "**Nowa wiadomość od USM Różany Potok**",
                    Telegram.Bot.Types.Enums.ParseMode.Markdown);
                await bot.SendTextMessageAsync(chatId, message);
            }
        }

        private async Task WriteTemperatureAndStateToDatabase()
        {
            if(!TryGetTemperature(out decimal temperature, out string rawData))
            {
                CircularLogger.Instance.Log("Error during adding new temperature sample to DB. Raw data: {1}{0}.", rawData, Environment.NewLine);
                return;
            }
            var database = Database.Instance;
            database.AddSample(new TemperatureSample { Date = DateTime.Now, Temperature = temperature });

            foreach (var entry in Globals.Relays)
            {
                var currentState = await entry.Value.Relay.TryGetStateAsync();
                if (!currentState.Success)
                {
                    continue;
                }

                var relaySample = new RelaySample(entry.Key, currentState.State);
                Database.Instance.AddSample(relaySample);
            }
        }

        private static string GetSender(Telegram.Bot.Types.User user)
        {
            return $"{user.FirstName} {user.LastName}";
        }

        private DateTime[] lastSpeakerHeartbeat;
        private DateTime startDate;
        private readonly Dictionary<string, PekaClient> pekaClients;
        private readonly TelegramBotClient bot;
        private readonly Authorizer authorizer;
        private readonly Stats stats;
        private static readonly CultureInfo PolishCultureInfo = new CultureInfo("pl-PL");               
    }
}
