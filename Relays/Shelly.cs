using System;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Relays
{
    public sealed class Shelly : HttpBasedRelay
    {
        public Shelly(string hostname) : base(hostname)
        {
            this.hostname = hostname;
        }

        protected override bool Toggle()
        {
            return FlurlClient.Request("relay/0?turn=toggle").GetJsonAsync().GetAwaiter().GetResult().ison;
        }

        protected override bool GetState()
        {
            var jsonState = FlurlClient.Request("relay/0").GetJsonAsync().GetAwaiter().GetResult();
            return jsonState.ison;
        }

        protected override void SetState(bool state)
        {
            var stateAsText = state ? "on" : "off";
            FlurlClient.Request($"relay/0?turn={stateAsText}").GetAsync().GetAwaiter().GetResult();
        }

        private readonly string hostname;
    }
}
