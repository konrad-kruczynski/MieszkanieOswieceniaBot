using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Chart : IGeneralCommand
	{
		public Chart(ITelegramBotClient bot)
		{
            this.bot = bot;
		}

        public Task ExecuteAsync(GeneralCommandParameters parameters)
        {
            var count = parameters.TakeInteger();
            var unit = parameters.TakeEnum<ChartUnit>();

            var (dateTimeFormat, timeBack) = unit switch
            {
                ChartUnit.H => ("HH:mm", TimeSpan.FromHours(count)),
                ChartUnit.D => ("ddd HH:mm", TimeSpan.FromDays(count)),
                ChartUnit.W => ("dd", TimeSpan.FromDays(7 * count)),
                ChartUnit.M => ("MMM", TimeSpan.FromDays(30 * count)),
                _ => throw new ParameterException(ParameterExceptionType.OutOfRangeError)
            };

            // TODO: last parameter, i.e. oneDay
            return CreateChart(timeBack, parameters.ChatId, dateTimeFormat, false);
        }

        private async Task CreateChart(TimeSpan timeBack, long chatId, string dateTimeFormat, bool oneDay)
        {
            var messageToEdit = await bot.SendTextMessageAsync(chatId, "Wykonuję...");
            var charter = new Charter(dateTimeFormat);
            var pngFile = await charter.PrepareChart(DateTime.Now - timeBack, DateTime.Now, oneDay,
                                               async step =>
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
                                               }, x => bot.SendTextMessageAsync(chatId, string.Format("Liczba próbek: {0}", x)));


            var fileToSend = new Telegram.Bot.Types.InputFiles.InputOnlineFile(File.OpenRead(pngFile));
            await bot.SendPhotoAsync(chatId, fileToSend);
            await bot.EditMessageTextAsync(chatId, messageToEdit.MessageId, "Gotowe.");

        }

        private readonly ITelegramBotClient bot;
    }

    public enum ChartUnit
    {
        H,
        D,
        W,
        M
    }
}

