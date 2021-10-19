using System;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Relays
{
    public sealed class Tasmota : IRelay
    {
        public Tasmota(string hostname)
        {
            flurlClient = new FlurlClient($"http://{hostname}").WithTimeout(TimeSpan.FromSeconds(10));
        }

        public bool State
        {
            get
            {
                return PowerStateToBool(flurlClient.Request("cm?cmnd=Power").GetJsonAsync().GetAwaiter().GetResult().POWER);
            }

            set
            {
                var commandValue = value ? "On" : "off";
                flurlClient.Request($"cm?cmnd=Power%20{commandValue}").GetAsync().GetAwaiter().GetResult();
            }
        }

        public bool Toggle()
        {
            var jsonResult = flurlClient.Request("/cm?cmnd=Power%20Toggle").GetJsonAsync().GetAwaiter().GetResult();
            return PowerStateToBool(jsonResult.POWER);
        }

        private readonly IFlurlClient flurlClient;

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
