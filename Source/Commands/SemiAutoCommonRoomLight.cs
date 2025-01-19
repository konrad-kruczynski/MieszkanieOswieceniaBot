using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
    public class SemiAutoCommonRoomLight : ITextCommand
    {
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            parameters.ExpectNoOtherParameters();

            var isOne = await Globals.Scenarios[1].TryCheckIfApplied();
            var isTwo = await Globals.Scenarios[2].TryCheckIfApplied();
            if (!isOne.Success || !isTwo.Success)
            {
                return "Nie udało się sprawdzić aktualnego scenariusza.";
            }

            if (isOne.Applied)
            {
                if (!await Globals.Scenarios[2].TryApplyAsync())
                {
                    return "Nie udało się sprawdzić aktualnego scenariusza.";
                }
            }
            else if (isTwo.Applied)
            {
                if (!await Globals.Scenarios[0].TryApplyAsync())
                {
                    return "Nie udało się sprawdzić aktualnego scenariusza.";
                }
            }
            else
            {
                if (!await Globals.Scenarios[1].TryApplyAsync())
                {
                    return "Nie udało się sprawdzić aktualnego scenariusza.";
                }
            }

            return "Wykonano.";
        }
    }
}

