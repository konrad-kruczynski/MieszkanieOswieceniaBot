using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Log : IGeneralCommand
	{
		public Log(ITelegramBotClient bot) 
		{
            this.bot = bot;
		}

        public async Task ExecuteAsync(Parameters parameters)
        {
            var entries = CircularLogger.Instance.GetEntriesAsHtmlStrings().ToList();
            if (entries.Count > 10)
            {
                entries.RemoveRange(0, entries.Count - 10);
            }

            foreach (var line in entries)
            {
                await bot.SendTextMessageAsync(parameters.ChatId, line, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
            }
        }

        private readonly ITelegramBotClient bot;
    }
}

