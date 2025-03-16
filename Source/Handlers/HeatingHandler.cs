using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Schedule;

namespace MieszkanieOswieceniaBot.Handlers;

public sealed class HeatingHandler : TimeBasedHandlerBase<decimal>
{
    public HeatingHandler(int valveId, bool enabled, params ScheduleEntry<decimal>[] switchingEvents) : base(enabled, switchingEvents)
    {
        this.valveId = valveId;
    }
    
    public HeatingHandler(int valveId, params ScheduleEntry<decimal>[] switchingEvents) : this(valveId, true, switchingEvents)
    {
    }
    
    protected override Task<bool> Activate(ScheduleEntry<decimal> currentEvent)
    {
        var valveEntry = Globals.Valves[valveId];
        var valve = valveEntry.Element;
        
        return valve.SetTemperature(currentEvent.Value);
    }

    protected override IEntryBase RelatedEntry => Globals.Valves[valveId];

    private readonly int valveId;
}