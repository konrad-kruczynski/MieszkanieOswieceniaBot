using System;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Newtonsoft;

namespace MieszkanieOswieceniaBot.Relays
{
    public class Shelly : HttpBasedRelay
    {
        public Shelly(string hostname, int relayNumber = 0) : this(hostname, "relay")
        {
            this.relayNumber = relayNumber;
        }

        protected Shelly(string hostname, string relayKey) : base(hostname)
        {
            this.relayKey = relayKey;
        }

        protected override async Task<bool> ToggleAsync()
        {
            return (await FlurlClient.Request($"{relayKey}/{relayNumber}?turn=toggle").GetJsonAsync().ConfigureAwait(false)).ison;
        }

        protected override async Task<bool> GetStateAsync()
        {
            var jsonState = await FlurlClient.Request($"{relayKey}/{relayNumber}").GetJsonAsync().ConfigureAwait(false);
            return jsonState.ison;
        }

        protected override Task SetStateAsync(bool state)
        {
            var stateAsText = state ? "on" : "off";
            return FlurlClient.Request($"{relayKey}/{relayNumber}?turn={stateAsText}").GetAsync();
        }

        private readonly int relayNumber;
        private readonly string relayKey;
    }
}
