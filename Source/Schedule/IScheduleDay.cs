using System;

namespace MieszkanieOswieceniaBot.Schedule;

public interface IScheduleDay
{
    bool IsApplicableFor(DateTime date);
}