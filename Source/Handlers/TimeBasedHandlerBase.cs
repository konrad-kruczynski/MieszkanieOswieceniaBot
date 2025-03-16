using System;
using System.Linq;
using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Schedule;

namespace MieszkanieOswieceniaBot.Handlers;

public abstract class TimeBasedHandlerBase<T> : IHandler
{
    protected TimeBasedHandlerBase(bool enabled, params ScheduleEntry<T>[] switchingEvents)
    {
        this.enabled = enabled;
        this.switchingEvents = switchingEvents;
    }
    
    public async Task RefreshAsync()
    {
        if (!enabled)
        {
            return;
        }

        var currentTime = DateTime.Now.TimeOfDay;
        var currentDayOfWeek = DateTime.Now.DayOfWeek;
        var currentEvent = switchingEvents.LastOrDefault(x => x.TimeOfDay <= currentTime && x.ScheduleDay.IsApplicableFor(DateTime.Now.Date));

        if (currentEvent == null)
        {
            // No state at this point of day, leave as is
            return;
        }

        if (lastSwitchAtTime == currentEvent.TimeOfDay && lastSwitchAtDayOfWeek == currentDayOfWeek)
        {
            return;
        }

        if(await Activate(currentEvent))
        {
            lastSwitchAtTime = currentEvent.TimeOfDay;
            lastSwitchAtDayOfWeek = currentDayOfWeek;
            CircularLogger.Instance.Log($"Setting relay '{RelatedEntry.FriendlyName}' to value/state = '{currentEvent.Value}'.");
        }
        else
        {
            CircularLogger.Instance.Log($"Could not set state/value to '{currentEvent.Value} 'for '{RelatedEntry.FriendlyName} with time = {currentEvent.TimeOfDay}'.");
        }
    }

    protected abstract Task<bool> Activate(ScheduleEntry<T> currentEvent);
    protected abstract IEntryBase RelatedEntry { get; }

    private readonly bool enabled;
    private readonly ScheduleEntry<T>[] switchingEvents;
    private TimeSpan lastSwitchAtTime;
    private DayOfWeek lastSwitchAtDayOfWeek;
}