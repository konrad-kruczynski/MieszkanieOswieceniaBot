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
            var originalState1 = await Globals.Relays[1].Element.TryGetStateAsync();
            var originalState2 = await Globals.Relays[2].Element.TryGetStateAsync();

            for (var i = 0; i < 10; i++)
            {
                await Globals.Relays[1].Element.TrySetStateAsync(true);
                await Task.Delay(TimeSpan.FromMilliseconds(40 * random.NextDouble()));
                await Globals.Relays[2].Element.TrySetStateAsync(true);
                await Task.Delay(TimeSpan.FromMilliseconds(400 * random.NextDouble()));
                await Globals.Relays[1].Element.TrySetStateAsync(false);
                await Task.Delay(TimeSpan.FromMilliseconds(40 * random.NextDouble()));
                await Globals.Relays[2].Element.TrySetStateAsync(false);
                await Task.Delay(TimeSpan.FromMilliseconds(200 * random.NextDouble()));
            }

            await Globals.Relays[1].Element.TrySetStateAsync(originalState1.State);
            await Globals.Relays[2].Element.TrySetStateAsync(originalState2.State);
            return "Wykonano.";
        }
    }
}

