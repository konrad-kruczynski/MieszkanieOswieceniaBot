using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
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
            lastSpeakerHeartbeat = DateTime.Now;
            authorizer = new Authorizer();
            bot.OnMessage += HandleMessage;
            bot.OnCallbackQuery += HandleCallbackQuery;
            bot.OnReceiveGeneralError += (sender, e) => HandleError(e.Exception.Message);
            bot.OnReceiveError += (sender, e) => HandleError(e.ApiRequestException.ToString());
        }

        public void Start()
        {
            bot.StartReceiving();
            var udpClient = new UdpClient(12345);
            Observable.FromAsync(udpClient.ReceiveAsync).Repeat().Subscribe(_ => lastSpeakerHeartbeat = DateTime.Now);
            Observable.Interval(TimeSpan.FromSeconds(10)).Subscribe(_ => RefreshSpeakerState());
        }

        private void HandleError(string error)
        {
            CircularLogger.Instance.Log("Bot error: {0}.", error);
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
                        await bot.SendTextMessageAsync(chatId, "Tylko administrator może takie rzeczy.");
                        CircularLogger.Instance.Log($"Unauthorized listing from {GetSender(e.Message.From)}.");
                        return;
                    }
                    var users = authorizer.ListUsers();
                    if(!users.Any())
                    {
                        await bot.SendTextMessageAsync(chatId, "Nie ma żadnych gadów.");
                    }
                    foreach(var user in users.Concat(Configuration.Instance.ListAdmins()))
                    {
                        var isAdmin = Configuration.Instance.IsAdmin(user);
                        var photos = (await bot.GetUserProfilePhotosAsync(user));
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

                var result = HandleTextCommand(e.Message);
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
                var noButton = new Telegram.Bot.Types.InlineKeyboardButton("Przeciwnie, chce go usunąć") { CallbackData = "r" + contactUserId };
                var keyboardMarkup = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup
                                                 (new[] { yesButton, noButton });

                await bot.SendTextMessageAsync(chatId, "Autoryzować gada?", replyMarkup: keyboardMarkup);
            }

            CircularLogger.Instance.Log("Unexpected (no-text) message from {0}.", GetSender(e.Message.From));
            return;
        }

        private async void HandleCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            stats.IncrementMessageCounter();
            if(!Configuration.Instance.IsAdmin(e.CallbackQuery.From.Id))
            {
                await bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "Tylko administrator może takie rzeczy.");
                CircularLogger.Instance.Log($"Trying to remove user by {GetSender(e.CallbackQuery.From)}.");
                return;
            }
            var empty = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new Telegram.Bot.Types.InlineKeyboardButton[0]);
            await bot.EditMessageReplyMarkupAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId, empty);
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
            await bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id,
                                           $"{operation} gada. Teraz jest ich {authorizer.ListUsers().Count()}.");
            await bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "Użyj komendy 'lista', aby obejrzeć kto to jest.");
        }

        private string HandleTextCommand(Telegram.Bot.Types.Message message)
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

            if(text == "log")
            {
                return CircularLogger.Instance.GetEntriesAsAString();
            }

            if(text == "ping")
            {
                return "pong";
            }

            CircularLogger.Instance.Log($"Unknown text command '{text}'.");
            return "Nieznana komenda.";
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

        private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromMinutes(1);

        private static readonly HashSet<int>[] Scenarios = new HashSet<int>[]
        {
                new HashSet<int>(),
                new HashSet<int> { 0, 1 },
                new HashSet<int> { 1, 2 },
                new HashSet<int> { 2 },
                new HashSet<int> { 1 },
                new HashSet<int> { 0 },
                new HashSet<int> { 0, 1, 2}
        };

        private static readonly Dictionary<int, string> FriendlyNames = new Dictionary<int, string>
        {
            { 0, "doniczka" },
            { 1, "lampa przy regale" },
            { 2, "lampa przy kanapie" }
        };
    }
}
