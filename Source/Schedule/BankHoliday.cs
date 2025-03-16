using System;
using System.Collections.Generic;
using PublicHoliday;

namespace MieszkanieOswieceniaBot.Schedule;

public class BankHoliday : IScheduleDay
{
    public bool IsApplicableFor(DateTime date)
    {
        return new PolandPublicHoliday().IsPublicHoliday(date);
    }
}