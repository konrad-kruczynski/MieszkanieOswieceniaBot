using System;
using System.Threading.Tasks;
using Humanizer;
using MieszkanieOswieceniaBot.Handlers;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class HearbeatProlonger : ITextCommand
	{
		public HearbeatProlonger(TimeSpan prolongValue, params HeartbeatenHandler[] handlers)
		{
            this.handlers = handlers;
            this.prolongValue = prolongValue;
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
            var prolongedTimeLeft = handler.ProlongedTimeLeft;
            if (prolongedTimeLeft <= TimeSpan.Zero)
            {
                return "Przyjęto.";
            }

            var friendlyName = handler.RelayEntry.FriendlyName;

            return $"{friendlyName}: wyłączenie za {prolongedTimeLeft.Humanize(culture: Globals.BotCommunicationCultureInfo)}.";
        }

        private readonly HeartbeatenHandler[] handlers;
        private readonly TimeSpan prolongValue;
    }
}

