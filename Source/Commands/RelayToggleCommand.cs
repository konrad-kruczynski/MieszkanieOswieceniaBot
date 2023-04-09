using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class RelayToggle : ITextCommand
	{
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            var relayNo = parameters.TakeString() switch
            {
                var integerString when int.TryParse(integerString, out var integer) => integer,
                _ => throw new NotImplementedException("Unknown relay for toggling")
            };

            var (success, currentState) = await Globals.Relays[relayNo].RelaySensor.TryToggleAsync();
            if (!success)
            {
                return "Nie udało się przełączyć stanu. Spróbuj ponownie później.";
            }

            return currentState switch
            {
                true => "Włączono",
                false => "Wyłączono"
            };
        }
    }
}

