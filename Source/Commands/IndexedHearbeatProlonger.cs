using System;
using System.Threading.Tasks;
using Humanizer;
using MieszkanieOswieceniaBot.Handlers;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class IndexedHeartbeatProlonger : ITextCommand
	{
		public IndexedHeartbeatProlonger(TimeSpan heartbeatProlongValue, params HeartbeatenHandler[] handlers)
		{
            this.handlers = handlers;
            this.prolongValue = heartbeatProlongValue;
		}

        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            HeartbeatenHandler handler;

            if (parameters.Count == 0)
            {
                handler = handlers[0];
            }
            else
            {
                var handlerIndex = parameters.TakeInteger() - 1;
                if (handlerIndex < 0 || handlerIndex > handlers.Length - 1)
                {
                    return "Niepoprawny numer do czuwania.";
                }

                handler = handlers[handlerIndex];
            }

            await handler.ProlongFor(prolongValue);
            return handler.GetFriendlyTimeOffValue();
        }

        private readonly HeartbeatenHandler[] handlers;
        private readonly TimeSpan prolongValue;
    }
}

