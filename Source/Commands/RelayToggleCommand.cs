using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class RelayToggle : ITextCommand
	{
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            var relayNo = parameters.CommandName switch
            {
                "r" => 7,
                _ => throw new NotImplementedException("Unknown relay for toggling")
            };

            var (success, currentState) = await Globals.Relays[relayNo].Relay.TryToggleAsync();
            if (!success)
            {
                return "Nie udało się przełączyć stanu. Spróbuj ponownie później.";
            }

            return currentState switch
            {
                true => "Światło włączono",
                false => "Światło wyłączono"
            };
        }
    }
}

