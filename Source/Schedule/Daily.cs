using System;

namespace MieszkanieOswieceniaBot.Schedule;

public sealed class Daily : IScheduleDay
{
    public bool IsApplicableFor(DateTime date)
    {
        return true;
    }
}