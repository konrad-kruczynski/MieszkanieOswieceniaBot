using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Export : IGeneralCommand
	{
		public Export(ITelegramBotClient bot)
		{
            this.bot = bot;
		}

        public async Task ExecuteAsync(Parameters parameters)
        {
            var chatId = parameters.ChatId;
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
        }

        private readonly ITelegramBotClient bot;
    }
}

