using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Relays
{
    public interface IRelay
    {
        Task<(bool Success, bool State)> TryGetStateAsync();
        Task<bool> TrySetStateAsync(bool state);
        Task<(bool Success, bool CurrentState)> TryToggleAsync();
    }
}
