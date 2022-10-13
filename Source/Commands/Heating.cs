using System;
using System.Linq;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Heating : ITextCommand
	{
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            int relayNo;
            if (!parameters.TryTakeString(out var whoToHeat))
            {
                var relayNos = new[] { 4, 5 };
                var relays = relayNos.Select(x => Globals.Relays[x].Relay);
                var tasks = relays.Select(x => x.GetFriendlyStateAsync()).ToArray();

                return string.Format("Kot: {0}{1}Kocica: {2}", await tasks[0], Environment.NewLine, await tasks[1]);
            }

            relayNo = whoToHeat switch
            {
                "kot" => 4,
                "kocica" => 5,
                _ => -1
            };

            if (relayNo == -1)
            {
                return "Niepoprawna informacja kogo grzać";
            }
            
            var (success, currentState) = await Globals.Relays[relayNo].Relay.TryToggleAsync();
            if (!success)
            {
                return "Nie udało się włączyć lub wyłączyć grzania. Spróbuj ponownie za jakiś czas.";
            }

            return currentState switch
            {
                true =>  "Grzanie włączono",
                false => "Grzanie wyłączono"
            };
        }
    }
}

