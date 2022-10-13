using System;
using System.Threading.Tasks;

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

        public TimeSpan ProlongedTimeLeft => lastHeartbeat - DateTime.UtcNow;

        public async Task RefreshAsync()
        {
            await Globals.Relays[relayId].Relay.TrySetStateAsync(DateTime.UtcNow - lastHeartbeat < timeout);
        }

        public RelayEntry RelayEntry => Globals.Relays[relayId];

        public Task HeartbeatAsync()
        {
            lastHeartbeat = DateTime.UtcNow;
            return RefreshAsync();
        }

        public Task ProlongFor(TimeSpan amount)
        {
            lastHeartbeat = (lastHeartbeat < DateTime.UtcNow ? DateTime.UtcNow : lastHeartbeat) + amount;
            return RefreshAsync();
        }

        private DateTime lastHeartbeat;
        private readonly int relayId;
        private readonly TimeSpan timeout;
    }
}

