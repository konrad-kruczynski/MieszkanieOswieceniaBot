using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
    public class Ping : ITextCommand
    {
        public Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            parameters.ExpectNoOtherParameters();
            return Task.FromResult("pong");
        }
    }
}

