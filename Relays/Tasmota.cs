using System;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Relays
{
    public sealed class Tasmota : HttpBasedRelay
    {
        public Tasmota(string hostname) : base(hostname, TimeSpan.FromSeconds(10))
        {
        }

        protected override bool Toggle()
        {
            var jsonResult = FlurlClient.Request("cm?cmnd=Power%20Toggle").GetJsonAsync().GetAwaiter().GetResult();
            return PowerStateToBool(jsonResult.POWER);
        }

        protected override bool GetState()
        {
            return PowerStateToBool(FlurlClient.Request("cm?cmnd=Power").GetJsonAsync().GetAwaiter().GetResult().POWER);
        }

        protected override void SetState(bool state)
        {
            var commandValue = state ? "On" : "off";
            FlurlClient.Request($"cm?cmnd=Power%20{commandValue}").GetAsync().GetAwaiter().GetResult();
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
