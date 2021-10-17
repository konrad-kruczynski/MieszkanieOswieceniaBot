using System;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Relays
{
    public sealed class Tasmota : IRelay
    {
        public Tasmota(string hostname)
        {
            this.hostname = hostname;
        }

        public bool State
        {
            get
            {
                return PowerStateToBool($"http://{hostname}/cm?cmnd=Power".GetJsonAsync().GetAwaiter().GetResult().POWER);
            }

            set
            {
                var commandValue = value ? "On" : "off";
                $"http://{hostname}/cm?cmnd=Power%20{commandValue}".GetAsync().GetAwaiter().GetResult();
            }
        }

        public bool Toggle()
        {
            var jsonResult = $"http://{hostname}/cm?cmnd=Power%20Toggle".GetJsonAsync().GetAwaiter().GetResult();
            return PowerStateToBool(jsonResult.POWER);
        }

        private readonly string hostname;

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
