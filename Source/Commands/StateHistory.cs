using System;
using System.Collections.Generic;
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
            var samplesStack = new Stack<RelaySample>();
            var oldStateFor = new bool?[Globals.Relays.Count];

            foreach (var sample in Database.Instance.TakeNewestSamples<RelaySample>())
            {
                if (oldStateFor.All(x => x.HasValue))
                {
                    break;
                }

                oldStateFor[sample.RelayId] = sample.State;
                samplesStack.Push(sample);
            }

            var lastKnownStateFor = new bool?[Globals.Relays.Count];

            while(!lastKnownStateFor.All(x => x.HasValue))
            {
                var sample = samplesStack.Pop();
                lastKnownStateFor[sample.RelayId] = sample.State;
            }

            var resultQueue = new Queue<string>();
            var currentStateFor = lastKnownStateFor.Select(x => x.Value).ToArray();

            while (samplesStack.TryPop(out var sample))
            {
                currentStateFor[sample.RelayId] = sample.State;

                while (samplesStack.TryPeek(out var consecutiveSample) && (consecutiveSample.Date - sample.Date) < TimeSpan.FromSeconds(5))
                {
                    sample = samplesStack.Pop();
                    lastKnownStateFor[sample.RelayId] = sample.State;
                }

                resultQueue.Enqueue(CreateStateLine(currentStateFor, sample.Date));
            }

            while (resultQueue.Count > 20)
            {
                resultQueue.Dequeue();
            }

            var result = new StringBuilder();
            foreach (var line in resultQueue)
            {
                result.AppendLine(line);
            }

            return Task.FromResult(result.ToString());
        }

        private static string CreateStateLine(bool[] states, DateTime date)
        {
            var resultString = new StringBuilder();
            resultString.AppendFormat("`{0:R}: ", date);
            for (var i = 0; i < states.Length; i++)
            {
                if (states[i])
                {
                    resultString.Append('●');
                }
                else
                {
                    resultString.Append('○');
                }
            }

            resultString.Append('`');
            return resultString.ToString();
        }
    }
}

