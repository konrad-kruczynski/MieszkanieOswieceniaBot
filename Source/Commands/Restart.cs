using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot.Commands
{
    [Privileged]
	public sealed class Restart : IGeneralCommand
	{
		public Restart(ITelegramBotClient bot)
		{
            this.bot = bot;
		}

        public async Task ExecuteAsync(Parameters parameters)
        {
            await bot.SendTextMessageAsync(parameters.ChatId, "Teraz restart");
            await Task.Delay(TimeSpan.FromSeconds(10));
            Environment.Exit(0);
        }

        private readonly ITelegramBotClient bot;
    }
}

