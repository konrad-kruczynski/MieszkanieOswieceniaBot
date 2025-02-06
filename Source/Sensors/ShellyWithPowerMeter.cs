using System;
using System.Threading.Tasks;
using Flurl.Http;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot.Sensors
{
	public class ShellyWithPowerMeter : Shelly, IPowerMeter
	{
        public ShellyWithPowerMeter(string hostname, int relayNumber = 0) : base(hostname, "relay")
        {
        }
        
        protected ShellyWithPowerMeter(string hostname, string relayKey) : base(hostname, relayKey)
        {
        }
        
        public async Task<(decimal, bool)> TryGetCurrentUsageAsync()
        {
            var result = await TryExecute(FlurlClient.Request("meter/0").GetJsonAsync());
            return ((decimal)result.Result.power, result.Success);
        }
    }
}

