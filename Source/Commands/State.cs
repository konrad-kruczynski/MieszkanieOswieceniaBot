using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using MieszkanieOswieceniaBot.Relays;
using MieszkanieOswieceniaBot.Sensors;

namespace MieszkanieOswieceniaBot.Commands
{
    public sealed class State : ITextCommand
    {
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            var result = new StringBuilder();
            var turnedOns = new List<string>();
            var unknowns = new List<string>();

            var relays = Globals.Relays.OrderBy(x => x.Key);
            var relaysWithStates = relays.Select(x => (x.Value, x.Value.RelaySensor.TryGetStateAsync())).ToArray();

            foreach (var (relay, stateTask) in relaysWithStates)
            {
                if (relay.RelaySensor is DefunctRelay)
                {
                    continue;
                }
                
                var state = await stateTask;
                result.Append($"{relay.Id} ({relay.FriendlyName}): {RelayExtensions.GetFriendlyStateFromSuccessAndState(state)}");
                if (state.State)
                {
                    result.Append(" od ");
                    result.Append(GetTurnedOnTime(relay.Id));

                    var correspondingHeartbeatenHandler = Globals.Heartbeatings.FirstOrDefault(x => x.RelayEntries.Any(y => y.Id == relay.Id));
                    if (correspondingHeartbeatenHandler != null)
                    {
                        var timeLeft = correspondingHeartbeatenHandler.ProlongedTimeLeft;
                        if (timeLeft > TimeSpan.Zero)
                        {
                            result.AppendFormat(", wyłączenie za {0}", timeLeft.Humanize(culture: Globals.BotCommunicationCultureInfo));
                        }
                    }

                    if (relay.RelaySensor is Relays.IDimmableRelay dimmableRelay)
                    {
                        var dimCheckResult = await dimmableRelay.GetDimValueAsync();
                        string dimValue;
                        if (dimCheckResult.Success)
                        {
                            dimValue = string.Format("{0}%", dimCheckResult.Value);
                        }
                        else
                        {
                            dimValue = "nieznana";
                        }

                        result.AppendFormat(", jasność {0}", dimValue);
                    }

                    if (relay.RelaySensor is IPowerMeter powerMeter)
                    {
                        var currentPowerUsage = await powerMeter.TryGetCurrentUsageAsync();
                        var currentPowerValue = currentPowerUsage.Success ? currentPowerUsage.Value.ToString("0.#") : "??";
                        
                        result.AppendFormat(", {0} W", currentPowerValue);
                    }
                }

                result.AppendLine();

                if (state.Success && state.State)
                {
                    turnedOns.Add(relay.FriendlyName);
                }

                if (!state.Success)
                {
                    unknowns.Add(relay.FriendlyName);
                }
            }

            if (turnedOns.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("Włączone:");

                foreach (var turnedOn in turnedOns)
                {
                    result.AppendLine(turnedOn);
                }
            }

            if (unknowns.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("Nieznane:");

                foreach (var unknown in unknowns)
                {
                    result.AppendLine(unknown);
                }
            }

            return result.ToString();
        }

        private string GetTurnedOnTime(int relayId)
        {
            var database = Database.Instance;
            using var enumerator = database.TakeNewestSamples<RelaySample>().GetEnumerator();
            while (enumerator.MoveNext())
            {
                var sample = enumerator.Current;
                if (sample.RelayId == relayId)
                {
                    if (sample.State)
                    {
                        return (DateTime.Now - sample.Date).Humanize(culture: Globals.BotCommunicationCultureInfo);
                    }
                    else
                    {
                        return "niedawna";
                    }
                }
            }

            return "???";
        }
    }
}

