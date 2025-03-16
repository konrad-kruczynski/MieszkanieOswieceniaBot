using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Power : ITextCommand
	{
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            var result = new StringBuilder();

            var sensorsWithCurrentUsages = Globals.PowerMeters.Select(x => (x.Value, x.Value.Element.TryGetCurrentUsageAsync())).ToArray();

            foreach (var (sensor, usageTask) in sensorsWithCurrentUsages.OrderBy(x => x.Item1.Id))
            {
                var usageResult = await usageTask;
                var powerValue = usageResult.Item2 ? (usageResult.Item1.ToString() + " W") : "nieznane";
                result.AppendLine($"{sensor.Id} ({sensor.FriendlyName}): {powerValue}");
            }

            return result.ToString();
        }
    }
}

