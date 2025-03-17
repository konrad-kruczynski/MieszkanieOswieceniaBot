using System;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Newtonsoft;

namespace MieszkanieOswieceniaBot.Relays
{
	public class ShellyDimmer : Shelly, IDimmableRelay
	{
		public ShellyDimmer(string hostname) : base(hostname, "light")
		{
		}

        public async Task<bool> DimToAsync(int value)
        {
            var result = await TryExecute(FlurlClient.Request($"light/0").SetQueryParam("brightness", value.ToString()).GetAsync());
            return result.Success;
        }

        public async Task<(bool Success, int Value)> GetDimValueAsync()
        {
            var result = await TryExecute(FlurlClient.Request("light/0").GetJsonAsync());
            return (result.Success, (int)result.Result.brightness);
        }

        protected override Task SetStateAsync(bool state)
        {
            if (state)
            {
                return FlurlClient.Request("light/0?turn=on").GetAsync();
            }

            return base.SetStateAsync(state);
        }
    }
}

