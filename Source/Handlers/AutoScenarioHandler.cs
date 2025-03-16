using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Relays;
using MieszkanieOswieceniaBot.Schedule;

namespace MieszkanieOswieceniaBot.Handlers
{
    public sealed class AutoScenarioHandler : TimeBasedHandlerBase<bool>
    {
        public AutoScenarioHandler(int relayId, bool enabled, params ScheduleEntry<bool>[] switchingEvents) : base(enabled, switchingEvents)
        {
            this.relayId = relayId;
        }
        
        public bool Enabled { get; set; } 
        
        protected override Task<bool> Activate(ScheduleEntry<bool> currentEvent)
        {
            var relayEntry = Globals.Relays[relayId];
            var relay = relayEntry.Element;

            return relay.TrySetStateAsync(currentEvent.Value);
        }

        private readonly int relayId;

        protected override IEntryBase RelatedEntry => Globals.Relays[relayId];
    }
}

