using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public interface ITextCommand : ICommand
	{
		Task<string> ExecuteAsync(Parameters parameters);
	}
}

