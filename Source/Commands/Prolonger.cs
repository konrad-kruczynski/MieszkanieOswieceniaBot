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
            var now = DateTimeOffset.UtcNow;
            if (now - lastUpdateAt < BounceThreshold)
            {
                return "Zignorowano";
            }

            lastUpdateAt = now;
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
        private DateTimeOffset lastUpdateAt;

        private static readonly TimeSpan BounceThreshold = TimeSpan.FromSeconds(2);

        private enum OffsetType
        {
            Current,
            Relative
        }
    } 
}

