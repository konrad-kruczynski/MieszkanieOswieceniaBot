using System;

namespace MieszkanieOswieceniaBot.Schedule;

public static class S
{
    public static IScheduleDay Date(DateTime specificDate) => new SpecificDate(specificDate);
    public static IScheduleDay Daily() => new Daily();
    public static IScheduleDay BankHoliday() => new BankHoliday();
    public static IScheduleDay On(params DayOfWeek[] daysOfWeek) => new WeekDay(daysOfWeek);
    public static IScheduleDay Or(params IScheduleDay[] days) => new Or(days);
    public static IScheduleDay And(params IScheduleDay[] days) => new And(days);
    
}