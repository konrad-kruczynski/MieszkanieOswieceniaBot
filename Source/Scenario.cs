﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot
{
    public sealed class Scenario
    {
        public Scenario(int[] coveredRange, int[] turnedOn, IReadOnlyDictionary<int, int> dimToValues = null)
        {
            this.coveredRange = new HashSet<int>(coveredRange);
            this.turnedOn = new HashSet<int>(turnedOn);
            this.dimToValues = new Dictionary<int, int>(dimToValues ?? new Dictionary<int, int>());

            if (!this.turnedOn.IsSubsetOf(this.coveredRange))
            {
                throw new InvalidOperationException("The turned on collection must be a subset of covered range.");
            }

            foreach (var dimToValue in this.dimToValues)
            {
                if (!this.turnedOn.Contains(dimToValue.Key))
                {
                    throw new InvalidOperationException("Relay to dim must be in the turned on collection.");
                }
            }
        }

        public async Task<(bool Success, bool Applied)> TryCheckIfApplied()
        {
            var relayEntries = Globals.Relays;
            
            foreach (var id in coveredRange)
            {
                var relayState = await relayEntries[id].Element.TryGetStateAsync();
                if(!relayState.Success)
                {
                    return (false, false);
                }

                if (turnedOn.Contains(id) ^ relayState.State)
                {
                    return (true, false);
                }
            }

            return (true, true);
        }

        public async Task<bool> TryApplyAsync()
        {
            var success = true;
            var relayEntries = Globals.Relays;

            foreach (var id in coveredRange)
            {
                var currentRelayState = await relayEntries[id].Element.TryGetStateAsync();
                if (!currentRelayState.Success)
                {
                    success = false;
                }
                else
                {
                    // nothing to do if already in a desired state
                    if (turnedOn.Contains(id) ^ currentRelayState.State)
                    {
                        if (!await relayEntries[id].Element.TrySetStateAsync(turnedOn.Contains(id)))
                        {
                            success = false;
                        }
                    }
                }

                if (relayEntries[id].Element is not IDimmableRelay dimmableRelay)
                {
                    continue;
                }
                
                var dimValue = dimToValues.GetValueOrDefault(id, 100);
                
                if (!await dimmableRelay.DimToAsync(dimValue))
                {
                    success = false;
                }
            }

            return success;
        }

        public string GetFriendlyDescription(IDictionary<int, IEntry<Relays.IRelay>> relayEntries)
        {
            if(turnedOn.Count == 0)
            {
                return "nic";
            }
            return turnedOn.Select(x => relayEntries[x].FriendlyName).Aggregate((x, y) => x + ", " + y);
        }

        private readonly HashSet<int> coveredRange;
        private readonly HashSet<int> turnedOn;
        private readonly Dictionary<int, int> dimToValues;
    }
}
