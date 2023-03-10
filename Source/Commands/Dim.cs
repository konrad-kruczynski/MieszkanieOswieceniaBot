using System;
using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot.Commands
{
	public class Dim : ITextCommand
	{
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            var dimToValue = parameters.TakeInteger();
            var relay = (IDimmableRelay)Globals.Relays[2].Relay;
            if (!await relay.DimToAsync(dimToValue))
            {
                return "Nie udało się ściemnić.";
            }

            return "Wykonano";
        }
    }
}

