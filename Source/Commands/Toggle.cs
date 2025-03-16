using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public class Toggle : ITextCommand
	{
		public Toggle(int relayNo)
		{
            this.relayNo = relayNo;
		}

        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            parameters.ExpectNoOtherParameters();
            await Globals.Relays[relayNo].Element.TryToggleAsync();
            return "Wykonano";
        }

        private readonly int relayNo;
    }
}

