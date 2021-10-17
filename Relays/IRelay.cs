using System;

namespace MieszkanieOswieceniaBot.Relays
{
    public interface IRelay
    {
        bool State { get; set; }
        bool Toggle();
    }
}
