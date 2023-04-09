using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Sensors
{
	public class TasmotaPowerMeter : HttpBasedPowerMeter
	{
        public TasmotaPowerMeter(string hostname) : base(hostname)
        {
        }

        protected override async Task<decimal> GetCurrentUsageAsync()
        {
            return (decimal)(await FlurlClient.Request("cm?cmnd=Status+10").GetJsonAsync().ConfigureAwait(false)).StatusSNS.ENERGY.Power;
        }
    }
}

