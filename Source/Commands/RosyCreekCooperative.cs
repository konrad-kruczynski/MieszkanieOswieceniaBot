using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot.Commands
{
    public sealed class RosyCreekCooperative : IGeneralCommand
    {
        public RosyCreekCooperative(ITelegramBotClient bot)
        {
            this.bot = bot;
        }

        public async Task ExecuteAsync(GeneralCommandParameters parameters)
        {
            if (parameters.Count == 0)
            {
                Database.Instance.AddHouseCooperativeChatId(parameters.ChatId);
                await bot.SendTextMessageAsync(parameters.ChatId, "Dodano");
                return;
            }

            var action = parameters.TakeEnum<RosyCreekAction>();

            switch (action)
            {
                case RosyCreekAction.Reset:
                    Database.Instance.NewestKnownRosyCreekNewsDate = DateTime.MinValue;
                    await bot.SendTextMessageAsync(parameters.ChatId, "Zresetowano");
                    return;
                default:
                    throw new ParameterException(ParameterExceptionType.OutOfRangeError);
            }
        }

        private readonly ITelegramBotClient bot;
    }

    public enum RosyCreekAction
    {
        Reset
    }
}

