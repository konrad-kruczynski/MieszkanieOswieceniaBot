using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public interface IGeneralCommand : ICommand
	{
		Task ExecuteAsync(GeneralCommandParameters parameters);
	}
}

