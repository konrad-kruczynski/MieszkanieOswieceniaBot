using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Relays
{
    public abstract class HttpBasedRelay : IRelay
    {
        protected HttpBasedRelay(string hostname, TimeSpan timeout = default)
        {
            var flurlClient = new FlurlClient($"http://{hostname}");
            if (timeout != default)
            {
                flurlClient = flurlClient.WithTimeout(timeout);
            }
            
            FlurlClient = flurlClient;
        }

        public async Task<(bool Success, bool State)> TryGetStateAsync()
        {
            try
            {
                var state = await GetStateAsync().ConfigureAwait(false);
                return (true, state);
            }
            catch (FlurlHttpException flurlException)
            {
                CircularLogger.Instance.Log($"Exception on {FlurlClient}: {flurlException.Message}");
                return (false, false);
            }
        }

        public async Task<bool> TrySetStateAsync(bool state)
        {
            try
            {
                await SetStateAsync(state).ConfigureAwait(false);
                return true;
            }
            catch (FlurlHttpException flurlException)
            {
                CircularLogger.Instance.Log($"Exception on {FlurlClient}: {flurlException}");
                return false;
            }
        }

        public async Task<(bool Success, bool CurrentState)> TryToggleAsync()
        {
            try
            {
                var currentState = await ToggleAsync().ConfigureAwait(false);
                return (true, currentState);
            }
            catch (FlurlHttpException flurlException)
            {
                CircularLogger.Instance.Log($"Exception on {FlurlClient}: {flurlException}");
                return (false, false);
            }
        }

        protected abstract Task<bool> ToggleAsync();
        protected abstract Task<bool> GetStateAsync();
        protected abstract Task SetStateAsync(bool state);

        protected readonly IFlurlClient FlurlClient;
    }
}
