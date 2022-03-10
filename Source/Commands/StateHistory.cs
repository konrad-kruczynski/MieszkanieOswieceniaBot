using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot.Commands
{
    public sealed class StateHistory : MarkdownTextCommand
    {
        public StateHistory(ITelegramBotClient bot) : base(bot)
        {
        }

        protected override Task<string> ExecuteInnerAsync(Parameters parameters)
        {
            var samples = Database.Instance.GetNewestSamples<RelaySample>(20 * Globals.Relays.Count);
            var samplesGroupedByMinutes = samples.GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, x.Date.Day, x.Date.Hour, x.Date.Minute, 0));
            samplesGroupedByMinutes = samplesGroupedByMinutes.OrderByDescending(x => x.Key).Take(20).OrderBy(x => x.Key);
            var resultString = new StringBuilder();
            var maximalRelayNumber = Globals.Relays.Max(x => x.Key);
            foreach (var group in samplesGroupedByMinutes)
            {
                resultString.AppendFormat("`{0:R}: ", group.Key);
                for (var i = 0; i <= maximalRelayNumber; i++)
                {
                    if (!Globals.Relays.ContainsKey(i))
                    {
                        resultString.Append('◌');
                    }
                    else if (group.Any(x => x.RelayId == i && x.State))
                    {
                        resultString.Append('●');
                    }
                    else
                    {
                        resultString.Append('○');
                    }
                }

                resultString.Append('`');
                resultString.AppendLine();
            }

            return Task.FromResult(resultString.ToString());
        }
    }
}

