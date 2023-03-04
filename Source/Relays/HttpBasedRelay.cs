using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Relays
{
    public abstract class HttpBasedRelay : IRelay
    {
        protected HttpBasedRelay(string hostname, TimeSpan timeout = default, bool cacheable = false)
        {
            var flurlClient = new FlurlClient($"http://{hostname}");
            if (timeout != default)
            {
                flurlClient = flurlClient.WithTimeout(timeout);
            }
            
            FlurlClient = flurlClient;
            this.cacheable = cacheable;
        }

        public async Task<(bool Success, bool State)> TryGetStateAsync()
        {
            if (cacheable && cachedValue.HasValue)
            {
                return (true, cachedValue.Value);
            }

            try
            {
                var state = await GetStateAsync().ConfigureAwait(false);
                if (cacheable)
                {
                    cachedValue = state;
                }

                return (true, state);
            }
            catch (Exception exception) when (exception is FlurlHttpException || exception is TaskCanceledException)
            {
                CircularLogger.Instance.Log($"Exception on {FlurlClient}: {exception.Message}");
                return (false, false);
            }
        }

        public async Task<bool> TrySetStateAsync(bool state)
        {
            try
            {
                await SetStateAsync(state).ConfigureAwait(false);
                if (cacheable)
                {
                    cachedValue = state;
                }

                return true;
            }
            catch (Exception exception) when (exception is FlurlHttpException || exception is TaskCanceledException)
            {
                CircularLogger.Instance.Log($"Exception on {FlurlClient}: {exception.Message}");
                return false;
            }
        }

        public async Task<(bool Success, bool CurrentState)> TryToggleAsync()
        {
            try
            {
                var currentState = await ToggleAsync().ConfigureAwait(false);
                if (cacheable)
                {
                    cachedValue = currentState;
                }

                return (true, currentState);
            }
            catch (Exception exception) when (exception is FlurlHttpException || exception is TaskCanceledException)
            {
                CircularLogger.Instance.Log($"Exception on {FlurlClient}: {exception.Message}");
                return (false, false);
            }
        }

        protected async Task<(bool Success, T Result)> TryExecute<T>(Task<T> task)
        {
            try
            {
                var state = await task;

                return (true, state);
            }
            catch (Exception exception) when (exception is FlurlHttpException || exception is TaskCanceledException)
            {
                CircularLogger.Instance.Log($"Exception on {FlurlClient}: {exception.Message}");
                return (false, default(T));
            }
        }

        protected abstract Task<bool> ToggleAsync();
        protected abstract Task<bool> GetStateAsync();
        protected abstract Task SetStateAsync(bool state);

        protected readonly IFlurlClient FlurlClient;

        private bool? cachedValue;
        private readonly bool cacheable;
    }
}
