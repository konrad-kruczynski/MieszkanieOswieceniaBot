using System;
using System.Collections.Generic;
using System.Linq;

namespace MieszkanieOswieceniaBot
{
    public sealed class Scenario
    {
        public Scenario(int[] coveredRange, int[] turnedOn)
        {
            this.coveredRange = new HashSet<int>(coveredRange);
            this.turnedOn = new HashSet<int>(turnedOn);

            if (!this.turnedOn.IsProperSubsetOf(coveredRange))
            {
                throw new InvalidOperationException("The turned on collection must be a propert subset of covered range.");
            }

        }

        public void Apply(IDictionary<int, RelayEntry> relayEntries)
        {
            foreach (var id in coveredRange)
            {
                relayEntries[id].Relay.State = turnedOn.Contains(id);
            }
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
