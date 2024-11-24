using System;
using MieszkanieOswieceniaBot.Schedule;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public class SchedulingTests
{
    [Test]
    public void ToCrontabSimpleAllWeek()
    {
        var entry = new ScheduleEntry<int>(0, "11:30",
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday);
        var crontabEntry = entry.ToShellyCrontabEntry();

        Assert.AreEqual("0 30 11 * * MON,TUE,WED,THU,FRI,SAT,SUN", crontabEntry);
    }

    [Test]
    public void ToCrontabSimpleOneDay()
    {
        var entry = new ScheduleEntry<int>(0, "7:00", DayOfWeek.Friday);
        var crontabEntry = entry.ToShellyCrontabEntry();

        Assert.AreEqual("0 0 7 * * FRI", crontabEntry);
    }

    [Test]
    public void ToCrontabSpecificDate()
    {
        var entry = new ScheduleEntry<int>(0, DateTime.Parse("2024-12-06 12:00"));
        var crontabEntry = entry.ToShellyCrontabEntry();
        
        Assert.AreEqual("0 0 12 6 12 *", crontabEntry);
    }

    [Test]
    public void ToCrontabClonedWithSpecificDate()
    {
        var entry = new ScheduleEntry<int>(0, "7:00", DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday);
        entry = entry.CloneWithSpecificDate(DateTime.Parse("2025-05-01"));
        var crontabEntry = entry.ToShellyCrontabEntry();
        
        Assert.AreEqual("0 0 7 1 5 *", crontabEntry);
    }

}