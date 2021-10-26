using System;

namespace MieszkanieOswieceniaBot
{
    public static class RelayExtensions
    {
        public static string GetFriendlyState(this Relays.IRelay relay)
        {
            if (!relay.TryGetState(out var state))
            {
                return "nieznany";
            }

            return state ? "włączony" : "wyłączony";
        }
    }
}
