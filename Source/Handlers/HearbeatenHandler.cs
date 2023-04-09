using System;
using System.Threading.Tasks;
using Humanizer;

namespace MieszkanieOswieceniaBot.Handlers
{
    public sealed class HeartbeatenHandler : IHandler
    {
        public HeartbeatenHandler(int relayId, TimeSpan timeout)
        {
            this.relayId = relayId;
            this.timeout = timeout;
            lastHeartbeat = new DateTime(2000, 1, 1).ToUniversalTime();
        }

        public TimeSpan ProlongedTimeLeft => lastHeartbeat - DateTimeOffset.UtcNow;

        public bool CurrentState => DateTimeOffset.UtcNow - lastHeartbeat < timeout;

        public async Task RefreshAsync()
        {
            await Globals.Relays[relayId].RelaySensor.TrySetStateAsync(CurrentState);
        }

        public IRelaySensorEntry<Relays.IRelay> RelayEntry => Globals.Relays[relayId];

        public Task HeartbeatAsync()
        {
            lastHeartbeat = DateTimeOffset.UtcNow;
            return RefreshAsync();
        }

        public Task ProlongFor(TimeSpan amount)
        {
            lastHeartbeat = (lastHeartbeat < DateTimeOffset.UtcNow ? DateTimeOffset.UtcNow : lastHeartbeat) + amount;
            return RefreshAsync();
        }

        public Task ProlongAtLeastTo(DateTimeOffset value)
        {
            if (value > lastHeartbeat)
            {
                lastHeartbeat = value;
            }
            return RefreshAsync();
        }

        public string GetFriendlyTimeOffValue()
        {
            var prolongedTimeLeft = ProlongedTimeLeft;
            if (prolongedTimeLeft <= TimeSpan.Zero)
            {
                return "Przyjęto.";
            }

            var friendlyName = RelayEntry.FriendlyName;

            return $"{friendlyName}: wyłączenie za {prolongedTimeLeft.Humanize(culture: Globals.BotCommunicationCultureInfo)}.";
        }

        private DateTimeOffset lastHeartbeat;
        private readonly int relayId;
        private readonly TimeSpan timeout;
    }
}

