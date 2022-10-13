using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
    public sealed class Temperature : ITextCommand
    {
        public Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            parameters.ExpectNoOtherParameters();
            if (!Controller.TryGetTemperature(out decimal temperature, out string rawData))
            {
                return Task.FromResult($"Błąd CRC, przekazuję gołe dane:\n{rawData}");
            }

            return Task.FromResult($"Temperatura wynosi {temperature:##.#}°C.");
        }
    }
}

