using System;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Newtonsoft;

namespace MieszkanieOswieceniaBot.Relays
{
    public class Shelly : HttpBasedRelay
    {
        public Shelly(string hostname) : this(hostname, "relay")
        {
            this.hostname = hostname;
        }

        protected Shelly(string hostname, string relayKey) : base(hostname)
        {
            this.relayKey = relayKey;
        }

        protected override async Task<bool> ToggleAsync()
        {
            return (await FlurlClient.Request($"{relayKey}/0?turn=toggle").GetJsonAsync().ConfigureAwait(false)).ison;
        }

        protected override async Task<bool> GetStateAsync()
        {
            var jsonState = await FlurlClient.Request($"{relayKey}/0").GetJsonAsync().ConfigureAwait(false);
            return jsonState.ison;
        }

        protected override Task SetStateAsync(bool state)
        {
            var stateAsText = state ? "on" : "off";
            return FlurlClient.Request($"{relayKey}/0?turn={stateAsText}").GetAsync();
        }

        private readonly string hostname;
        private readonly string relayKey;
    }
}
