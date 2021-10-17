using System;

namespace MieszkanieOswieceniaBot
{
    public static class RelayExtensions
    {
        public static string GetFriendlyState(this Relays.IRelay relay) => relay.State ? "właczone" : "wyłączone";
    }
}
