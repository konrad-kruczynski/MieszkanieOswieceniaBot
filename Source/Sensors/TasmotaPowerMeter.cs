using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Sensors
{
	public sealed class TasmotaPowerMeter : IPowerMeter
	{
		public TasmotaPowerMeter(string hostname)
		{
            flurlClient = new FlurlClient($"http://{hostname}");
        }

        public async Task<(decimal, bool)> TryGetCurrentUsageAsync()
        {
            try
            {
                var value = (decimal)(await flurlClient.Request("cm?cmnd=Status+10").GetJsonAsync().ConfigureAwait(false)).StatusSNS.ENERGY.Power;
                return (value, true);
            }
            catch (Exception exception) when (exception is FlurlHttpException || exception is TaskCanceledException)
            {
                CircularLogger.Instance.Log($"Exception on {flurlClient}: {exception.Message}");
                return (default(decimal), false);
            }
        }

        private readonly IFlurlClient flurlClient;
    }
}

