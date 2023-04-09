using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Sensors
{
	public abstract class HttpBasedPowerMeter : IPowerMeter
	{
		protected HttpBasedPowerMeter(string hostname)
		{
            FlurlClient = new FlurlClient($"http://{hostname}");
        }

        public async Task<(decimal, bool)> TryGetCurrentUsageAsync()
        {
            try
            {
                var value = await GetCurrentUsageAsync();
                return (value, true);
            }
            catch (Exception exception) when (exception is FlurlHttpException || exception is TaskCanceledException)
            {
                CircularLogger.Instance.Log($"Exception on {FlurlClient}: {exception.Message}");
                return (default(decimal), false);
            }
        }

        protected abstract Task<decimal> GetCurrentUsageAsync();

        protected readonly IFlurlClient FlurlClient;
    }
}

