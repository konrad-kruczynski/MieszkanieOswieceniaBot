using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Alarm : ITextCommand
	{
        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            parameters.ExpectNoOtherParameters();
            var random = new Random();
            var originalState1 = await Globals.Relays[1].RelaySensor.TryGetStateAsync();
            var originalState2 = await Globals.Relays[2].RelaySensor.TryGetStateAsync();

            for (var i = 0; i < 10; i++)
            {
                await Globals.Relays[1].RelaySensor.TrySetStateAsync(true);
                await Task.Delay(TimeSpan.FromMilliseconds(40 * random.NextDouble()));
                await Globals.Relays[2].RelaySensor.TrySetStateAsync(true);
                await Task.Delay(TimeSpan.FromMilliseconds(400 * random.NextDouble()));
                await Globals.Relays[1].RelaySensor.TrySetStateAsync(false);
                await Task.Delay(TimeSpan.FromMilliseconds(40 * random.NextDouble()));
                await Globals.Relays[2].RelaySensor.TrySetStateAsync(false);
                await Task.Delay(TimeSpan.FromMilliseconds(200 * random.NextDouble()));
            }

            await Globals.Relays[1].RelaySensor.TrySetStateAsync(originalState1.State);
            await Globals.Relays[2].RelaySensor.TrySetStateAsync(originalState2.State);
            return "Wykonano.";
        }
    }
}

