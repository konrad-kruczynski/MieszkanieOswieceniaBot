using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot
{
    public sealed class Scenario
    {
        public Scenario(int[] coveredRange, int[] turnedOn)
        {
            this.coveredRange = new HashSet<int>(coveredRange);
            this.turnedOn = new HashSet<int>(turnedOn);

            if (!this.turnedOn.IsSubsetOf(coveredRange))
            {
                throw new InvalidOperationException("The turned on collection must be a subset of covered range.");
            }

        }

        public async Task<bool> TryApplyAsync(IDictionary<int, RelayEntry> relayEntries)
        {
            var success = true;

            foreach (var id in coveredRange)
            {
                if (!await relayEntries[id].Relay.TrySetStateAsync(turnedOn.Contains(id)))
                {
                    success = false;
                }
            }

            return success;
        }

        public string GetFriendlyDescription(IDictionary<int, RelayEntry> relayEntries)
        {
            if(turnedOn.Count == 0)
            {
                return "nic";
            }
            return turnedOn.Select(x => relayEntries[x].FriendlyName).Aggregate((x, y) => x + ", " + y);
        }

        private readonly HashSet<int> coveredRange;
        private readonly HashSet<int> turnedOn;
    }
}
