using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot.Commands
{
    public sealed class Notifications : IGeneralCommand
    {
        public Notifications(ITelegramBotClient bot)
        {
            this.bot = bot;
        }

        public async Task ExecuteAsync(GeneralCommandParameters parameters)
        {
            if (parameters.Count == 0)
            {
                Database.Instance.AddNotificationChatId(parameters.ChatId);
                await bot.SendTextMessageAsync(parameters.ChatId, "Dodano");
                return;
            }

            var action = parameters.TakeEnum<NotificationAction>();

            switch (action)
            {
                case NotificationAction.Reset:
                    Database.Instance.NewestKnownRosyCreekNewsDate = DateTime.MinValue;
                    await bot.SendTextMessageAsync(parameters.ChatId, "Zresetowano");
                    return;
                default:
                    throw new ParameterException(ParameterExceptionType.OutOfRangeError);
            }
        }

        private readonly ITelegramBotClient bot;
    }

    public enum NotificationAction
    {
        Reset
    }
}

