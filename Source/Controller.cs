using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Handlers;
using MieszkanieOswieceniaBot.OtherDevices;
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
            authorizer = new Authorizer();

            Globals.Heartbeatings[0].SetActivationAction(9, async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                await Globals.Infrareds[0].SendInfrared(0x807FC03F, 0x1FE03FC);
                await Task.Delay(TimeSpan.FromSeconds(3));
                await Globals.Infrareds[0].SendInfrared(0x807F20DF, 0x1FE04FB);
            });
            
            commandRegister = InitializeCommandRegister();
            apiHandler = new ApiHandler();
            CircularLogger.Instance.Log("Bot initialized.");
        }

        public CommandRegister InitializeCommandRegister()
        {
            var register = new CommandRegister(bot);

            register.RegisterCommandListCommandAs("komendy");
            register.RegisterCommand("ping", new Commands.Ping());
            register.RegisterCommand("grzanie", new Commands.Heating());
            register.RegisterCommand("czas", new Commands.Time());
            register.RegisterCommand("peka", new Commands.Peka(bot));
            register.RegisterCommand("odpady", new Commands.Waste());
            register.RegisterCommand("stan", new Commands.State());
            register.RegisterCommand("histogram", new Commands.Histogram(bot));
            register.RegisterCommand("wykres", new Commands.Chart(bot));
            register.RegisterCommand("historia", new Commands.TemperatureHistory(bot));
            register.RegisterCommand("historia2", new Commands.StateHistory(bot));
            register.RegisterCommand("eksport", new Commands.Export(bot));
            register.RegisterCommand("lista", new Commands.UserList(bot));
            register.RegisterCommand("restart", new Commands.Restart(bot));
            register.RegisterCommand("log", new Commands.Log(bot));
            register.RegisterCommand("bitcoin", new Commands.Bitcoin());
            register.RegisterCommand("powiadomienia", new Commands.Notifications(bot));

            var statsCommand = new Commands.Stats(stats);
            register.RegisterCommand("staty", statsCommand);
            register.RegisterCommand("statystyki", statsCommand);

            var relayToggleCommand = new Commands.RelayToggle();
            register.RegisterCommand("p", relayToggleCommand);

            var externalLampProlongTime = TimeSpan.FromMinutes(15);
            var externalLampProlonger = new Commands.IndexedHeartbeatProlonger(externalLampProlongTime, Globals.Heartbeatings[2]);
            register.RegisterCommand("z", externalLampProlonger);
            var externalLampParametrizedProlonger = new Commands.Prolonger(Globals.Heartbeatings[2]);
            register.RegisterCommand("z_param", externalLampParametrizedProlonger);
            var externalLampLongProlonger = new Commands.IndexedHeartbeatProlonger(TimeSpan.FromHours(2), Globals.Heartbeatings[2]);
            register.RegisterCommand("z1", externalLampLongProlonger);
            // actually a hacky way to switch it off
            var externalLampTurnOffHandler = new Commands.IndexedHeartbeatProlonger(TimeSpan.FromDays(-365), Globals.Heartbeatings[2]);
            register.RegisterCommand("z0", externalLampTurnOffHandler);

            var heartbeatProlongerCommand = new Commands.IndexedHeartbeatProlonger(TimeSpan.FromHours(1), Globals.Heartbeatings[0..2]);
            register.RegisterCommand("czuwanie", heartbeatProlongerCommand);
            var hearbeatAntiProlongerCommand = new Commands.IndexedHeartbeatProlonger(TimeSpan.FromHours(-1), Globals.Heartbeatings[0..2]);
            register.RegisterCommand("antyczuwanie", hearbeatAntiProlongerCommand);

            var alarmCommand = new Commands.Alarm();
            register.RegisterCommand("alarm", alarmCommand);
            register.RegisterCommand("miganie", alarmCommand);

            var temperatureCommand = new Commands.Temperature();
            register.RegisterCommand("temp", temperatureCommand);
            register.RegisterCommand("temperature", temperatureCommand);

            for (var i = 0; i < 8; i++)
            {
                register.RegisterCommand(i.ToString(), new Commands.Scenario(i));
            }

            var removeLastTemperatureSamples = new Commands.RemoveLastTempSamples();
            register.RegisterCommand("usuwaniePróbek", removeLastTemperatureSamples);

            var semiAutoLight = new Commands.SemiAutoCommonRoomLight();
            register.RegisterCommand("światło", semiAutoLight);
            register.RegisterCommand("swiatlo", semiAutoLight);

            var dimCommand = new Commands.Dim();
            register.RegisterCommand("dim", dimCommand);

            var powerCommand = new Commands.Power();
            register.RegisterCommand("moc", powerCommand);

            var christmasCommand = new Commands.Toggle(10);
            register.RegisterCommand("lampki", christmasCommand);

            return register;
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

        private void SubscribeOnRandomInterval(TimeSpan min, TimeSpan max, SynchronizationContext context, Func<Task> handler)
        {
            RandomInterval(min, max).Select(x => Observable.FromAsync(handler).ObserveOn(context))
                .Concat().Subscribe();
        }

        public static IObservable<int> RandomInterval(TimeSpan min, TimeSpan max)
        {
            return Observable.Generate(0, i => true, i => 0, i => 0,
                i => TimeSpan.FromMilliseconds(new Random().Next((int)min.TotalMilliseconds, (int)max.TotalMilliseconds)));
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

            apiHandler.Start();

            Observable.FromAsync(apiHandler.GetNextRequestAsync).Repeat().ObserveOn(SynchronizationContext.Current)
                .Select(x => Observable.FromAsync(async () => await HandleHttp(x)).ObserveOn(SynchronizationContext.Current))
                .Concat().Subscribe();

            SubscribeOnInterval(TimeSpan.FromMinutes(2), SynchronizationContext.Current, WriteTemperatureAndStateToDatabase);
            SubscribeOnInterval(TimeSpan.FromMinutes(1), SynchronizationContext.Current, RefreshHandlers);
            SubscribeOnInterval(TimeSpan.FromHours(8), SynchronizationContext.Current, CheckHousingCooperativeNews);
            SubscribeOnRandomInterval(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), SynchronizationContext.Current, CheckWasherStatus);

            CircularLogger.Instance.Log("Bot started.");

            while (true)
            {
                try
                {
                    await foreach (var update in new Telegram.Bot.Extensions.Polling.QueuedUpdateReceiver(bot))
                    {
                        await HandleUpdate(update);
                    }
                }
                catch (Exception e)
                {
                    CircularLogger.Instance.Log("Error during receiving Telegram message: {0}.", e.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }

        private async Task HandleHttp((Guid Id, string CommandName, IReadOnlyDictionary<string, string> Parameters) request)
        {
            var result = await commandRegister.HandleApiRequestAsync(request.CommandName, request.Parameters);
            apiHandler.AddResponseFor(request.Id, result.Status, result.Message);
        }

        private async Task HandleUdp(UdpReceiveResult result)
        {
            var bufferAsString = Encoding.UTF8.GetString(result.Buffer);
            if(int.TryParse(bufferAsString, out var number))
            {
                if(number == 10)
                {
                    await Globals.Relays[3].RelaySensor.TrySetStateAsync(true);
                }

                if(number == 11)
                {
                    await Globals.Relays[3].RelaySensor.TrySetStateAsync(false);
                }

                // TODO
                //await HandleScenarioAsync(number);
                return;
            }

            // TODO:
            await Globals.Heartbeatings[0].HeartbeatAsync();
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
                // TODO: move other message types away from here
                // authorization can then be removed

                await commandRegister.HandleTelegramMessageAsync(message);
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

            CircularLogger.Instance.Log("Unexpected (non-text) message from {0}.", GetSender(message.From));
            return;
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

        // TODO: move
        internal static bool TryGetTemperature(out decimal temperature, out string rawData)
        {
            try
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
                temperature = Math.Round(temperature, 1);
                return true;
            }
            catch (IOException e)
            {
                CircularLogger.Instance.Log($"Error during getting temperature: {e.Message}.");
                temperature = default;
                rawData = default;
                return false;
            }
        }

        private async Task RefreshHandlers()
        {
            IEnumerable<Handlers.IHandler> handlers = Globals.AutoScenarios;
            handlers = handlers.Concat(Globals.Heartbeatings);

            foreach (Handlers.IHandler handler in handlers)
            {
                await handler.RefreshAsync();
            }
        }

        private async Task CheckWasherStatus()
        {
            const int MaxWasherQueueLength = 5;
            const int PowerThreshold = 3;

            var (powerUsage, success) = await Globals.PowerMeters[0].RelaySensor.TryGetCurrentUsageAsync();
            if (!success)
            {
                return;
            }

            washerPowerUsage.Enqueue(powerUsage);

            if (washerPowerUsage.Count > MaxWasherQueueLength)
            {
                washerPowerUsage.Dequeue();
            }

            var washerIsRunning = washerPowerUsage.Any(x => x > PowerThreshold);

            if (!washerIsRunning && lastPowerWasherStatus)
            {
                var chatIds = Database.Instance.GetNotificationChatIds();
                foreach (var chatId in chatIds)
                {
                    await bot.SendTextMessageAsync(chatId, "Pralka zakończyła pracę.");
                }
            }

            lastPowerWasherStatus = washerIsRunning;
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
            var chatIds = Database.Instance.GetNotificationChatIds();
            foreach(var chatId in chatIds)
            {
                await bot.SendTextMessageAsync(chatId, "**Nowa wiadomość od USM Różany Potok**",
                    Telegram.Bot.Types.Enums.ParseMode.Markdown);
                await bot.SendTextMessageAsync(chatId, message);
            }
        }

        private async Task WriteTemperatureAndStateToDatabase()
        {
            var database = Database.Instance;

            if (TryGetTemperature(out decimal temperature, out string rawData))
            {
                database.AddSample(new TemperatureSample { Date = DateTime.Now, Temperature = temperature });
            }
            else
            {
                CircularLogger.Instance.Log("Error during adding new temperature sample to DB. Raw data: {1}{0}.", rawData, Environment.NewLine);
            }

            foreach (var entry in Globals.Relays)
            {
                var currentState = await entry.Value.RelaySensor.TryGetStateAsync();
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
            return $"{user.FirstName} {user.LastName} ({user.Id})";
        }

        private Queue<decimal> washerPowerUsage = new Queue<decimal>();
        private bool lastPowerWasherStatus;
        private DateTime startDate;
        private readonly TelegramBotClient bot;
        private readonly Authorizer authorizer;
        private readonly Stats stats;
        private readonly CommandRegister commandRegister;
        private readonly ApiHandler apiHandler;
    }
}
