using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class Stats : ITextCommand
	{
        public Stats(MieszkanieOswieceniaBot.Stats stats)
        {
            this.stats = stats;
        }

        public Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            parameters.ExpectNoOtherParameters();
            return Task.FromResult(stats.GetStats());
        }

        private readonly MieszkanieOswieceniaBot.Stats stats;
    }
}

