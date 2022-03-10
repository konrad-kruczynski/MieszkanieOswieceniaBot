using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Time : ITextCommand
	{
        public Task<string> ExecuteAsync(Parameters parameters)
        {
            return Task.FromResult(DateTime.Now.ToString());
        }
    }
}

