using System;
using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot.Commands
{
	public class Evening : ITextCommand
    { 
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            var currentState = await Globals.Relays[2].Relay.TryGetStateAsync();
            if (!currentState.Success)
            {
                return "Nie udało się odczytać stanu.";
            }

            if (!currentState.State)
            {
                if (!await Globals.Relays[2].Relay.TrySetStateAsync(true))
                {
                    return "Nie udało się ustawić stanu";
                }
            }

            var dimmableRelay = (IDimmableRelay)Globals.Relays[2].Relay;
            var currentBrightness = await dimmableRelay.GetDimValueAsync();
            if (!currentBrightness.Success)
            {
                return "Nie udało się odczytać jasności";
            }

            if (currentBrightness.Value != DimmedBrightness)
            {
                if (!await dimmableRelay.DimToAsync(DimmedBrightness))
                {
                    return "Nie udało się ustawić jasności";
                }
            }
            else
            {
                if (!await dimmableRelay.TrySetStateAsync(false))
                {
                    return "Nie udało się ustawić stanu";
                }
            }

            return "Wykonano";
        }

        private const int DimmedBrightness = 50;
    }
}

