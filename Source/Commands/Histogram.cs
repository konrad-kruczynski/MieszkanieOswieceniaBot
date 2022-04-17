using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Histogram : IGeneralCommand
	{
        public Histogram(ITelegramBotClient bot)
        {
            this.bot = bot;
        }

        public async Task ExecuteAsync(Parameters parameters)
        {
            if (parameters.Count == 0)
            {
                throw new ParameterException(ParameterExceptionType.NotEnoughParameters);
            }

            var relayNumbers = new List<int>();
            for (var i = 0; i < parameters.Count; i++)
            {
                relayNumbers.Add(parameters.TakeInteger());
            }

            await CreateHistogram(parameters.ChatId, relayNumbers);
        }

        private async Task CreateHistogram(long chatId, List<int> relayNos)
        {
            var names = relayNos.Select(x => Globals.Relays[x].FriendlyName);
            var messageToEdit = await bot.SendTextMessageAsync(chatId, "Wykonuję..."); ;
            var charter = new Charter("");
            var pngFile = await charter.PrepareHistogram(relayNos, async step =>
            {
                switch (step)
                {
                    case Step.RetrievingData:
                        await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Pobieranie danych...");
                        break;
                    case Step.CreatingPlot:
                        await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Tworzenie wykresu...");
                        break;
                    case Step.RenderingImage:
                        await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Renderowanie obrazu...");
                        break;

                }
            });

            var fileToSend = new Telegram.Bot.Types.InputFiles.InputOnlineFile(File.OpenRead(pngFile));
            await bot.SendPhotoAsync(chatId, fileToSend);
            await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Gotowe.");

        }

        private readonly ITelegramBotClient bot;
    }
}

