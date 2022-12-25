using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class RemoveLastTempSamples : ITextCommand
	{
        public Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            var count = parameters.TakeInteger();
            return Task.FromResult(
                string.Format("Usunięto {0} próbek",
                    Database.Instance.RemoveLast<TemperatureSample>(count).ToString()));
        }
    }
}
