using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Sensors
{
	public class ShellyPowerMeter : HttpBasedPowerMeter
	{
        public ShellyPowerMeter(string hostname) : base(hostname)
        {
        }

        protected override async Task<decimal> GetCurrentUsageAsync()
        {
            return (decimal)(await FlurlClient.Request("meter/0").GetJsonAsync().ConfigureAwait(false)).power;
        }
    }
}

