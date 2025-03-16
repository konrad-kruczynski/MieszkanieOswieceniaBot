using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MieszkanieOswieceniaBot.Commands;
using PeanutButter.Utils;

namespace MieszkanieOswieceniaBot.Schedule;

public sealed class ScheduleEntry<T>
{
    public ScheduleEntry(T value, string time, IScheduleDay scheduleDay)
    {
        this.value = value;
        this.ScheduleDay = scheduleDay;
        timeOfDay = TimeSpan.Parse(time);
        if (timeOfDay > TimeSpan.FromHours(24) || timeOfDay.Seconds != 0)
        {
            throw new ArgumentException($"{nameof(time)} must represent time of day without the seconds component.");
        }
    }
    
    public T Value => value;
    public TimeSpan TimeOfDay => timeOfDay;
    public IScheduleDay ScheduleDay { get; }

    private readonly TimeSpan timeOfDay;
    private readonly T value;
}