using System;

namespace MieszkanieOswieceniaBot.Relays
{
    public interface IRelay
    {
        bool TryGetState(out bool state);
        bool TrySetState(bool state);
        bool TryToggle(out bool currentState);
    }
}
