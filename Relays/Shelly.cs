using System;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Relays
{
    public sealed class Shelly : IRelay
    {
        public Shelly(string hostname)
        {
            this.hostname = hostname;
        }

        public bool State
        {
            get
            {
                var jsonState = $"http://{hostname}/relay/0".GetJsonAsync().GetAwaiter().GetResult();
                return jsonState.ison;
            }

            set
            {
                var stateAsText = value ? "on" : "off";
                $"http://{hostname}/relay/0?turn={stateAsText}".GetAsync().GetAwaiter().GetResult();
            }
        }

        public bool Toggle()
        {
            return $"http://{hostname}/relay/0?turn=toggle".GetJsonAsync().GetAwaiter().GetResult().ison;
        }

        private readonly string hostname;
    }
}
