using System;
using System.Linq;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot
{
    public sealed class AutoScenarioHandler
    {
        public AutoScenarioHandler(int relayId, bool enabled, params (string, bool)[] switchingEvents)
        {
            this.switchingEvents = switchingEvents.Select(x => (TimeSpan.Parse(x.Item1), x.Item2)).OrderBy(x => x.Item1).ToArray();
            this.relayId = relayId;
            Enabled = enabled;
        }

        public bool Enabled { get; set; }

        public void Refresh()
        {
            if (!Enabled)
            {
                return;
            }

            var currentTime = DateTime.Now.TimeOfDay;
            var currentEvent = switchingEvents.Where(x => x.Item1 <= currentTime).LastOrDefault();

            if (currentEvent == default((TimeSpan, bool)))
            {
                // No state at this point of day, leave as is
                return;
            }

            if (lastSwitchAt == currentEvent.Item1)
            {
                return;
            }

            var relayEntry = Globals.Relays[relayId];
            var relay = relayEntry.Relay;

            if (relay.TrySetState(currentEvent.Item2))
            {
                lastSwitchAt = currentEvent.Item1;
            }
            else
            {
                CircularLogger.Instance.Log($"Could not set auto scenario state to {currentEvent.Item2} for '{relayEntry.FriendlyName}'.");
            }
        }

        private TimeSpan lastSwitchAt;
        private readonly (TimeSpan, bool)[] switchingEvents;
        private readonly int relayId;
    }
}

