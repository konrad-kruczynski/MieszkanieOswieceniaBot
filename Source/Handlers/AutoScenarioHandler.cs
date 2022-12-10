using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot.Handlers
{
    public sealed class AutoScenarioHandler : IHandler
    {
        public AutoScenarioHandler(int relayId, bool enabled, params (HashSet<DayOfWeek>, string, bool)[] switchingEvents)
        {
            this.switchingEvents = switchingEvents.Select(x => (new HashSet<DayOfWeek>(x.Item1), TimeSpan.Parse(x.Item2), x.Item3)).OrderBy(x => x.Item1).ToArray();
            this.relayId = relayId;
            Enabled = enabled;
        }

        public AutoScenarioHandler(int relayId, bool enabled, params (string, bool)[] switchingEvents)
            : this(relayId, enabled, switchingEvents.Select(x => (AllDays, x.Item1, x.Item2)).ToArray()) { }
        
        public bool Enabled { get; set; }

        public async Task RefreshAsync()
        {
            if (!Enabled)
            {
                return;
            }

            var currentTime = DateTime.Now.TimeOfDay;
            var currentDayOfWeek = DateTime.Now.DayOfWeek;
            var currentEvent = switchingEvents.Where(x => x.Item2 <= currentTime && x.Item1.Contains(currentDayOfWeek)).LastOrDefault();

            if (currentEvent == default((HashSet<DayOfWeek>, TimeSpan, bool)))
            {
                // No state at this point of day, leave as is
                return;
            }

            if (lastSwitchAtTime == currentEvent.Item2 && lastSwitchAtDayOfWeek == currentDayOfWeek)
            {
                return;
            }

            var relayEntry = Globals.Relays[relayId];
            var relay = relayEntry.Relay;

            if (await relay.TrySetStateAsync(currentEvent.Item3))
            {
                lastSwitchAtTime = currentEvent.Item2;
                lastSwitchAtDayOfWeek = currentDayOfWeek;
                CircularLogger.Instance.Log($"Setting relay '{relayEntry.FriendlyName}' to state = '{currentEvent.Item3}'.");
            }
            else
            {
                CircularLogger.Instance.Log($"Could not set auto scenario state to {currentEvent.Item2} for '{relayEntry.FriendlyName}'.");
            }
        }

        private static HashSet<DayOfWeek> AllDays => new HashSet<DayOfWeek>(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday });

        private TimeSpan lastSwitchAtTime;
        private DayOfWeek lastSwitchAtDayOfWeek;
        private readonly (HashSet<DayOfWeek>, TimeSpan, bool)[] switchingEvents;
        private readonly int relayId;
    }
}

