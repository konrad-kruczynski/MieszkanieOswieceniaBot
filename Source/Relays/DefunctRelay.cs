using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Relays
{
	public class DefunctRelay : IRelay
	{
		public DefunctRelay()
		{
		}

        public Task<(bool Success, bool State)> TryGetStateAsync()
        {
            return Task.FromResult((true, false));
        }

        public Task<bool> TrySetStateAsync(bool state)
        {
            return Task.FromResult(false);
        }

        public Task<(bool Success, bool CurrentState)> TryToggleAsync()
        {
            return Task.FromResult((false, false));
        }
    }
}

