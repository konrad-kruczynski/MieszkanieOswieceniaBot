using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MieszkanieOswieceniaBot.Commands
{
	public abstract class MarkdownTextCommand : IGeneralCommand
	{
		protected MarkdownTextCommand(ITelegramBotClient bot)
		{
            this.bot = bot;
		}

        public async Task ExecuteAsync(Parameters parameters)
        {
            var result = await ExecuteInnerAsync(parameters);
            await bot.SendTextMessageAsync(parameters.ChatId, result, parseMode: ParseMode.Markdown);
        }

        protected abstract Task<string> ExecuteInnerAsync(Parameters parameters);

        private readonly ITelegramBotClient bot;
    }
}

