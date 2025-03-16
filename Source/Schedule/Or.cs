using System;
using System.Linq;

namespace MieszkanieOswieceniaBot.Schedule;

public sealed class Or : IScheduleDay
{
    public Or(params IScheduleDay[] ors)
    {
        this.days = ors.ToArray();
    }

    public bool IsApplicableFor(DateTime date)
    {
        return days.Any(day => day.IsApplicableFor(date));
    }

    private readonly IScheduleDay[] days; 
}