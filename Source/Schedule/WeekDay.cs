using System;
using System.Collections.Generic;
using PeanutButter.Utils;

namespace MieszkanieOswieceniaBot.Schedule;

public sealed class WeekDay : IScheduleDay
{
    public WeekDay(params DayOfWeek[] days)
    {
        daysOfWeek = days.AsHashSet();
    }
    
    public bool IsApplicableFor(DateTime date)
    {
        return daysOfWeek.Contains(date.DayOfWeek);
    }
    
    private readonly HashSet<DayOfWeek> daysOfWeek;
}