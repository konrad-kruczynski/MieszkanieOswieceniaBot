using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public class SemiAutoCommonRoomLight : ITextCommand
	{
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            parameters.ExpectNoOtherParameters();

            var hourOfDay = DateTime.Now.Hour;
            var isEvening = hourOfDay >= 20 || hourOfDay <= 6;
            var scenario = isEvening ? Globals.Scenarios[2] : Globals.Scenarios[1];
            var isApplied = await scenario.TryCheckIfApplied(Globals.Relays);
            if (!isApplied.Success)
            {
                return "Nie udało się sprawdzić aktualnego scenariusza.";
            }

            if (!isApplied.Applied)
            {
                await scenario.TryApplyAsync(Globals.Relays);
            }
            else
            {
                await Globals.Scenarios[0].TryApplyAsync(Globals.Relays);
            }

            return "Wykonano.";
        }
    }
}

