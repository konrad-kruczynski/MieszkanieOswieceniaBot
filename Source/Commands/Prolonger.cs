using System;
using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Handlers;

namespace MieszkanieOswieceniaBot.Commands
{
    public sealed class Prolonger : ITextCommand
    {
        public Prolonger(HeartbeatenHandler handler)
        {
            this.handler = handler;
        }

        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            var offsetType = parameters.TakeEnum<OffsetType>();
            var offsetValue = parameters.TakeInteger();

            if (offsetType == OffsetType.Current)
            {
                await handler.ProlongAtLeastTo(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(offsetValue));
            }
            else
            {
                await handler.ProlongFor(TimeSpan.FromMinutes(offsetValue));
            }

            return handler.GetFriendlyTimeOffValue();
        }

        private readonly HeartbeatenHandler handler;

        private enum OffsetType
        {
            Current,
            Relative
        }
    } 
}

