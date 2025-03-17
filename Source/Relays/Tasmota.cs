using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Relays
{
    public sealed class Tasmota : HttpBasedRelay
    {
        public Tasmota(string hostname, bool cacheable = false) : base(hostname, TimeSpan.FromSeconds(10), cacheable)
        {
        }

        protected override async Task<bool> ToggleAsync()
        {
            var jsonResult = await FlurlClient.Request("cm?cmnd=Power%20Toggle").GetJsonAsync().ConfigureAwait(false);
            return PowerStateToBool(jsonResult.POWER);
        }

        protected override async Task<bool> GetStateAsync()
        {
            return PowerStateToBool((await FlurlClient.Request("cm?cmnd=Power").GetJsonAsync().ConfigureAwait(false)).POWER);
        }

        protected override Task SetStateAsync(bool state)
        {
            var commandValue = state ? "On" : "off";
            return FlurlClient.Request($"cm?cmnd=Power%20{commandValue}").GetAsync();
        }

        private static bool PowerStateToBool(string state)
        {
            switch(state)
            {
                case "ON":
                    return true;
                default:
                    return false;
            }
        }
    }
}
