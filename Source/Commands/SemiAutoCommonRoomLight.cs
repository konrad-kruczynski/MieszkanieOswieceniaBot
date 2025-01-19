using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
    public class SemiAutoCommonRoomLight : ITextCommand
    {
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {

            var key = parameters.TakeInteger();
            parameters.ExpectNoOtherParameters();

            if (!scenarioMaps.TryGetValue(key, out var scenarioCycle))
            {
                return "Niewałściwy klucz cyklu scenariuszy";
            }

            // no scenario will work as last scenario found >> activate the first one
            var currentScenario = scenarioCycle.Length - 1;
            for (var i = 0; i < scenarioCycle.Length; i++)
            {
                var (success, isApplied) = await Globals.Scenarios[scenarioCycle[i]].TryCheckIfApplied();
                if (!success)
                {
                    return "Nie udało się sprawdzić aktualnego scenariusza.";
                }

                if (isApplied)
                {
                    currentScenario = i;
                    break;
                }
            }
            
            var scenarioToApply = scenarioCycle[(currentScenario + 1) % scenarioCycle.Length];

            if (!await Globals.Scenarios[scenarioToApply].TryApplyAsync())
            {
                return "Nie udało się wykonać scenariusza, a przynajmniej nie w całości.";
            }

            return "Wykonano";
        }

        private readonly Dictionary<int, int[]> scenarioMaps = new()
        {
            { 0, new[] { 1, 2 } },
            { 1, new[] { 3, 4 } },
        };
    }
}

