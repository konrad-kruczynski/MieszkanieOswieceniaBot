using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Relays
{
    public sealed class Shelly : HttpBasedRelay
    {
        public Shelly(string hostname) : base(hostname)
        {
            this.hostname = hostname;
        }

        protected override async Task<bool> ToggleAsync()
        {
            return (await FlurlClient.Request("relay/0?turn=toggle").GetJsonAsync().ConfigureAwait(false)).ison;
        }

        protected override async Task<bool> GetStateAsync()
        {
            var jsonState = await FlurlClient.Request("relay/0").GetJsonAsync().ConfigureAwait(false);
            return jsonState.ison;
        }

        protected override Task SetStateAsync(bool state)
        {
            var stateAsText = state ? "on" : "off";
            return FlurlClient.Request($"relay/0?turn={stateAsText}").GetAsync();
        }

        private readonly string hostname;
    }
}
