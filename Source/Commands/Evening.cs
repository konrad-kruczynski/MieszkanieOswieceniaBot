using System;
using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot.Commands
{
	public class Evening : ITextCommand
    { 
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            var relay = (IDimmableRelay)Globals.Relays[2].Element;
            var currentState = await relay.TryGetStateAsync();
            if (!currentState.Success)
            {
                return "Nie udało się odczytać stanu.";
            }

            if (!currentState.State)
            {
                if (!await relay.TrySetStateAsync(true))
                {
                    return "Nie udało się ustawić stanu";
                }

                return "Wykonano";
            }

            
            var currentBrightness = await relay.GetDimValueAsync();
            if (!currentBrightness.Success)
            {
                return "Nie udało się odczytać jasności";
            }

            if (currentBrightness.Value != DimmedBrightness)
            {
                if (!await relay.DimToAsync(DimmedBrightness))
                {
                    return "Nie udało się ustawić jasności";
                }
            }
            else
            {
                if (!await relay.TrySetStateAsync(false))
                {
                    return "Nie udało się ustawić stanu";
                }
            }

            return "Wykonano";
        }

        private const int DimmedBrightness = 50;
    }
}

