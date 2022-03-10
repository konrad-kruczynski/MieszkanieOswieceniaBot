using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace MieszkanieOswieceniaBot.Commands
{
    [Privileged]
	public sealed class UserList : IGeneralCommand
	{
		public UserList(ITelegramBotClient bot)
		{
            this.bot = bot;
            authorizer = new Authorizer();
		}

        public async Task ExecuteAsync(Parameters parameters)
        {
            var users = authorizer.ListUsers();
            foreach (var user in users.Concat(Configuration.Instance.ListAdmins()))
            {
                var isAdmin = Configuration.Instance.IsAdmin(user);
                var photos = await bot.GetUserProfilePhotosAsync(user);
                if (photos.TotalCount < 1)
                {
                    continue;
                }

                var photo = photos.Photos[0][0];

                var markup = new InlineKeyboardMarkup(
                    new[] { InlineKeyboardButton.WithCallbackData("Usuń", "r" + user) });
                var photoToSend = new Telegram.Bot.Types.InputFiles.InputOnlineFile(photo.FileId.ToString());
                await bot.SendPhotoAsync(parameters.ChatId, photoToSend, isAdmin ? "Administrator" : "Użytkownik",
                                         replyMarkup: isAdmin ? null : markup);
            }
        }

        private readonly Authorizer authorizer;
        private readonly ITelegramBotClient bot;
    }
}

