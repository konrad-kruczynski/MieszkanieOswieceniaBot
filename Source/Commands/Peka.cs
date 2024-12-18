using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot.Commands
{
    public sealed class Peka : IGeneralCommand
    {
        public Peka(ITelegramBotClient telegramBotClient)
        {
            pekaClients = new Dictionary<string, PekaClient>();
            bot = telegramBotClient;
        }

        public async Task ExecuteAsync(GeneralCommandParameters parameters)
        {
            foreach (var pekaEntry in PekaDb.Instance.GetData())
            {
                if (!pekaClients.TryGetValue(pekaEntry.Item2, out var client))
                {
                    client = new PekaClient(pekaEntry.Item2, pekaEntry.Item3);
                    pekaClients[pekaEntry.Item2] = client;
                }
                var balance = await client.GetCurrentBalance();
                await bot.SendMessage(parameters.ChatId, string.Format("{0}: {1:0.00} PLN", pekaEntry.Item1, balance));
            }
        }

        private readonly Dictionary<string, PekaClient> pekaClients;
        private readonly ITelegramBotClient bot;
    }
}

