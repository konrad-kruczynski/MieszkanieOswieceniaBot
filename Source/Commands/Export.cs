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

        public async Task ExecuteAsync(GeneralCommandParameters parameters)
        {
            var chatId = parameters.ChatId;
            var progressMessage = await bot.SendMessage(chatId, "Przygotowuję...");
            var lastMessage = string.Empty;
            var exportFile = await Database.Instance.GetTemperatureSampleExport(async progress =>
            {
                var message = $"Wykonuję ({100 * progress:0}%)...";
                if (message != lastMessage)
                {
                    await bot.EditMessageText(chatId, progressMessage.MessageId, message);
                    lastMessage = message;
                }
            });
            await bot.EditMessageText(chatId, progressMessage.MessageId, "Wysyłam...");
            var fileToSend = new Telegram.Bot.Types.InputFileStream(File.OpenRead(exportFile), "probki.json.gz");
            await bot.SendDocument(chatId, fileToSend);
            await bot.EditMessageText(chatId, progressMessage.MessageId, "Gotowe");
        }

        private readonly ITelegramBotClient bot;
    }
}

