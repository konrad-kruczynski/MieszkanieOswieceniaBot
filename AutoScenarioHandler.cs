using System;
using System.Linq;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot
{
	public sealed class AutoScenarioHandler
	{
		public AutoScenarioHandler(IRelay relay, params (string, bool)[] switchingEvents)
		{
			this.switchingEvents = switchingEvents.Select(x => (TimeSpan.Parse(x.Item1), x.Item2)).OrderBy(x => x.Item1).ToArray();
			this.relay = relay;
		}

		public void Refresh()
        {
			var currentTime = DateTime.Now.TimeOfDay;
			var currentEvent = switchingEvents.Where(x => x.Item1 <= currentTime).LastOrDefault();
			if(lastSwitchAt == currentEvent.Item1)
			{
				return;
			}

			if(relay.TrySetState(currentEvent.Item2))
			{
				lastSwitchAt = currentEvent.Item1;
			}
			else
			{
				CircularLogger.Instance.Log("Could not set auto scenario state.");
			}
        }

		private TimeSpan lastSwitchAt;
		private readonly (TimeSpan, bool)[] switchingEvents;
		private readonly IRelay relay;
	}
}

