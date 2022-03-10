using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
    public sealed class State : ITextCommand
    {
        public async Task<string> ExecuteAsync(Parameters parameters)
        {
            var result = new StringBuilder();
            var turnedOns = new List<string>();
            var unknowns = new List<string>();

            var relays = Globals.Relays.OrderBy(x => x.Key);
            var relaysWithStates = relays.Select(x => (x.Value, x.Value.Relay.TryGetStateAsync())).ToArray();

            foreach (var (relay, stateTask) in relaysWithStates)
            {
                var state = await stateTask;
                result.AppendLine($"{relay.Id} ({relay.FriendlyName}): {RelayExtensions.GetFriendlyStateFromSuccessAndState(state)}");
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
    }
}

