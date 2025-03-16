using System;

namespace MieszkanieOswieceniaBot.Schedule;

public sealed class SpecificDate : IScheduleDay
{
    public SpecificDate(DateTime specificDate)
    {
        this.specificDate = specificDate.Date;
    }
    
    public bool IsApplicableFor(DateTime date)
    {
        return date.Date == specificDate.Date;
    }
    
    private readonly DateTime specificDate;
}