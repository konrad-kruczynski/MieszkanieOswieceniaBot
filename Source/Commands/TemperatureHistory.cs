using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot.Commands
{
    public sealed class TemperatureHistory : MarkdownTextCommand
    {
        public TemperatureHistory(ITelegramBotClient bot) : base(bot)
        {
        }

        protected override Task<string> ExecuteInnerAsync(Parameters parameters)
        {
            parameters.ExpectNoOtherParameters();
            var samples = Database.Instance.GetNewestSamples<TemperatureSample>(30);
            return Task.FromResult(samples.Select(x => "`" + x.ToString() + "`").Aggregate((x, y) => x + Environment.NewLine + y));
        }
    }
}

