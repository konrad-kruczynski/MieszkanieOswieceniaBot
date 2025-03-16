using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MieszkanieOswieceniaBot.Handlers;
using MieszkanieOswieceniaBot.Heating;
using MieszkanieOswieceniaBot.OtherDevices;
using MieszkanieOswieceniaBot.Schedule;

namespace MieszkanieOswieceniaBot
{
    internal static class Globals
    {
        public static readonly Dictionary<int, IEntry<Relays.IRelay>> Relays = new IEntry<Relays.IRelay>[]
        {
            Entry.Create(0, new Relays.Uart("/dev/ttyUSB1", 0), "lampa doniczka"),
            Entry.Create(1, new Relays.Uart("/dev/ttyUSB1", 1), "lampa stojąca"),
            Entry.Create(2, new Relays.ShellyDimmer("192.168.71.39"), "lampa przy kanapie"),
            Entry.Create(3, new Relays.Uart("/dev/ttyUSB0", 0), "głośniki w salonie"),
            Entry.Create(4, new Relays.Shelly("192.168.71.38"), "mata grzejna prawa"),
            Entry.Create(5, new Relays.Shelly("192.168.71.37"), "mata grzejna lewa"),
            Entry.Create(6, new Relays.Shelly("192.168.71.34"), "lampa zewnętrzna"),
            Entry.Create(7, new Relays.DefunctRelay(), "oświetlenie akwarium"),
            Entry.Create(8, new Relays.Tasmota("192.168.71.36", true), "głośniki w sypialni"),
            Entry.Create(9, new Relays.Tasmota("192.168.71.31", true), "Cambridge Audio DAC"),
            Entry.Create(10, new Relays.Tasmota("192.168.71.35", true), "lampki choinkowe")
        }.ToDictionary(x => x.Id, x => x);

        public static readonly Dictionary<int, IEntry<Sensors.IPowerMeter>> PowerMeters = new IEntry<Sensors.IPowerMeter>[]
        {
            Entry.Create(0, new Sensors.TasmotaPowerMeter("192.168.71.32"), "pralka"),
            Entry.Create(1, new Sensors.ShellyPowerMeter("192.168.71.38"), "mata grzejna prawa"),
            Entry.Create(2, new Sensors.ShellyPowerMeter("192.168.71.37"), "mata grzejna lewa"),
            Entry.Create(3, new Sensors.TasmotaPowerMeter("192.168.71.31"), "Cambridge Audio DAC"),
        }.ToDictionary(x => x.Id, x => x);

        public static readonly Dictionary<int, IEntry<Heating.IValve>> Valves = new IEntry<IValve>[]
        {
            Entry.Create(0, new Heating.Shelly(22, 17, "192.168.71.40"), "grzejnik w gabinecie"),
        }.ToDictionary(x => x.Id, x => x);

        public static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(30);

        private static readonly int[] BasicRange = new[] { 0, 1, 2 };

        public static readonly Scenario[] Scenarios = new Scenario[]
        {
            new Scenario(BasicRange, Array.Empty<int>()),
            new Scenario(BasicRange, new [] { 0, 1 }),
            new Scenario(BasicRange, new [] { 1, 2 }),
            new Scenario(BasicRange, new [] { 2 }),
            new Scenario(BasicRange, new [] { 1 }),
            new Scenario(BasicRange, new [] { 0 }),
            new Scenario(BasicRange, new [] { 0, 1, 2 }),
            new Scenario(BasicRange, new [] { 0, 2 }),
        };

        public static readonly AutoScenarioHandler[] AutoScenarios = new AutoScenarioHandler[]
        {
            // disable bed heating on weekends as "normal" heating is triggered on that days
            new AutoScenarioHandler(4, false, new ScheduleEntry<bool>(false, "7:00", new WeekDay(DayOfWeek.Saturday))),
            new AutoScenarioHandler(5, false, new ScheduleEntry<bool>(false, "7:00", new WeekDay(DayOfWeek.Saturday)))
        };

        public static readonly HeartbeatenHandler[] Heartbeatings = new[]
        {
            new HeartbeatenHandler(HeartbeatTimeout, 3, 9),
            new HeartbeatenHandler(HeartbeatTimeout, 8),
            new HeartbeatenHandler(TimeSpan.Zero, 6) // we do not really use "heartbeat" feature here
        };
        
        public static readonly HeatingHandler[] HeatingHandlers = new[] {
            new HeatingHandler(0,
                new ScheduleEntry<decimal>(22, "6:00", S.On(DayOfWeek.Monday, DayOfWeek.Friday)),
                new ScheduleEntry<decimal>(17, "17:00", S.On(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday)),
                new ScheduleEntry<decimal>(17, "23:00", S.Daily())
                )
        };

        public static readonly IInfraredReceiverSender[] Infrareds = new[]
        {
            new TasmotaInfrared("192.168.71.41"),
        };

        public static readonly CultureInfo BotCommunicationCultureInfo = new CultureInfo("pl-PL");
    }
}