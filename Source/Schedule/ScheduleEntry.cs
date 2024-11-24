using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MieszkanieOswieceniaBot.Commands;
using PeanutButter.Utils;

namespace MieszkanieOswieceniaBot.Schedule;

public class ScheduleEntry<T>
{
    public ScheduleEntry(T value, string time, params DayOfWeek[] days)
    {
        this.value = value;
        timeOfDay = TimeSpan.Parse(time);
        if (timeOfDay > TimeSpan.FromHours(24) || timeOfDay.Seconds != 0)
        {
            throw new ArgumentException($"{nameof(time)} must represent time of day without the seconds component.");
        }
        
        daysOfWeek = ExtensionsForIEnumerables.ToHashSet(days);
    }
    
    public ScheduleEntry(T value, DateTime specificDate)
    {
        timeOfDay = specificDate.TimeOfDay;
        this.specificDate = specificDate.Subtract(timeOfDay);
    }

    public ScheduleEntry<T> CloneWithSpecificDate(DateTime date)
    {
        if (date.TimeOfDay != TimeSpan.Zero)
        {
            throw new AggregateException($"{nameof(specificDate)} must not have the time of a day component.");
        }
        
        date = date.Add(timeOfDay);
        return new ScheduleEntry<T>(value, date);
    }
    
    public T Value => value;

    public string ToShellyCrontabEntry()
    {
        if (daysOfWeek == null)
        {
            // specific date mode
            return $"{timeOfDay.Seconds} {timeOfDay.Minutes} {timeOfDay.Hours} {specificDate.Day} {specificDate.Month} *";
        }
        
        var daysOfWeekString = daysOfWeek.OrderBy(x => ((int)x + 6)%7)
            .Select(x => x.ToString().ToUpperInvariant()[..3])
            .Aggregate((x, y) => $"{x},{y}");

        var builder = new StringBuilder();
        builder.Append(0);
        builder.Append(' ');
        builder.Append(timeOfDay.Minutes);
        builder.Append(' ');
        builder.Append(timeOfDay.Hours);
        builder.Append(" * * ");
        
        return builder + daysOfWeekString;
    }

    private readonly TimeSpan timeOfDay;
    private readonly HashSet<DayOfWeek> daysOfWeek;
    private readonly DateTime specificDate;
    private readonly T value;
}