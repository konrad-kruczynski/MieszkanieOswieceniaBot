using System;
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

        public bool TryGetState(out bool state)
        {
            try
            {
                state = GetState();
                return true;
            }
            catch (FlurlHttpException flurlException)
            {
                CircularLogger.Instance.Log($"Exception on {FlurlClient}: {flurlException.Message}");
                state = false;
                return false;
            }
        }

        public bool TrySetState(bool state)
        {
            try
            {
                SetState(state);
                return true;
            }
            catch(FlurlHttpException flurlException)
            {
                CircularLogger.Instance.Log($"Exception on {FlurlClient}: {flurlException}");
                return false;
            }
        }

        public bool TryToggle(out bool currentState)
        {
            try
            {
                currentState = Toggle();
                return true;
            }
            catch(FlurlHttpException flurlException)
            {
                CircularLogger.Instance.Log($"Exception on {FlurlClient}: {flurlException}");
                currentState = false;
                return false;
            }
        }

        protected abstract bool Toggle();
        protected abstract bool GetState();
        protected abstract void SetState(bool state);

        protected readonly IFlurlClient FlurlClient;
    }
}
