using System;
using System.Linq;

namespace MieszkanieOswieceniaBot.Schedule;

public sealed class And : IScheduleDay
{
    public And(params IScheduleDay[] days)
    {
        this.days = days.ToArray();
    }
    
    public bool IsApplicableFor(DateTime date)
    {
        return days.All(day => day.IsApplicableFor(date));
    }
    
    private readonly IScheduleDay[] days;
}