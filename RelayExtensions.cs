using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot
{
    public static class RelayExtensions
    {
        public static async Task<string> GetFriendlyStateAsync(this Relays.IRelay relay)
        {
            var result = await relay.TryGetStateAsync();
            if (!result.Success)
            {
                return "nieznany";
            }

            return result.State ? "włączony" : "wyłączony";
        }

        public static string GetFriendlyStateFromSuccessAndState((bool Success, bool State) input)
        {
            if (!input.Success)
            {
                return "nieznany";
            }

            return input.State ? "włączony" : "wyłączony";
        }
    }
}
