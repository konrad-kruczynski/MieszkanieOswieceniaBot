using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Heating : ITextCommand
	{
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            int relayNo;
            if (!parameters.TryTakeString(out var whichToHeat))
            {
                var relayNos = new[] { 4, 5 };
                var relays = relayNos.Select(x => Globals.Relays[x].RelaySensor);
                var tasks = relays.Select(x => x.GetFriendlyStateAsync()).ToArray();

                return string.Format("Prawa: {0}{1}Lewa: {2}", await tasks[0], Environment.NewLine, await tasks[1]);
            }

            relayNo = whichToHeat switch
            {
                "prawa" => 4,
                "lewa" => 5,
                "monika" => 4,
                "konrad" => 5,
                _ => -1
            };

            if (relayNo == -1)
            {
                return "Niepoprawna informacja o tym, którą matę włączyć";
            }
            
            var (success, currentState) = await Globals.Relays[relayNo].RelaySensor.TryToggleAsync();
            if (!success)
            {
                return "Nie udało się włączyć lub wyłączyć grzania. Spróbuj ponownie za jakiś czas.";
            }

            var relevantPowerMeter = Globals.PowerMeters[RelayNoToPowerMeterNo[relayNo]].RelaySensor;

            if (!currentState)
            {
                return "Grzanie wyłączono";
            }
            
            await Task.Delay(GracePeriod);
            (var powerValue, success) = await relevantPowerMeter.TryGetCurrentUsageAsync();
            if (!success)
            {
                return "Nie udało się stwierdzić, czy mata rzeczywiście zaczęła grzać";
            }

            return powerValue < PowerThreshold ? "Pomimo włączenia grzania mata nie uruchomiła się" : "Grzanie włączono";
        }

        private static readonly Dictionary<int, int> RelayNoToPowerMeterNo = new() { { 4, 1 }, { 5, 2 } };
        private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(5);
        private const decimal PowerThreshold = 5m;
    }
}

