using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MieszkanieOswieceniaBot.Handlers;
using MieszkanieOswieceniaBot.OtherDevices;

namespace MieszkanieOswieceniaBot
{
    internal static class Globals
    {
        public static readonly Dictionary<int, IRelaySensorEntry<Relays.IRelay>> Relays = new IRelaySensorEntry<Relays.IRelay>[]
        {
            RelaySensorEntry.Create(0, new Relays.Uart("/dev/ttyUSB1", 0), "lampa doniczka"),
            RelaySensorEntry.Create(1, new Relays.Uart("/dev/ttyUSB1", 1), "lampa stojąca"),
            RelaySensorEntry.Create(2, new Relays.ShellyDimmer("192.168.71.39"), "lampa materiałowa"),
            RelaySensorEntry.Create(3, new Relays.Uart("/dev/ttyUSB0", 0), "głośniki w salonie"),
            RelaySensorEntry.Create(4, new Relays.Shelly("192.168.71.38"), "mata grzejna prawa"),
            RelaySensorEntry.Create(5, new Relays.Shelly("192.168.71.37"), "mata grzejna lewa"),
            RelaySensorEntry.Create(6, new Relays.Shelly("192.168.71.34"), "lampa zewnętrzna"),
            RelaySensorEntry.Create(7, new Relays.DefunctRelay(), "oświetlenie akwarium"),
            RelaySensorEntry.Create(8, new Relays.Tasmota("192.168.71.36", true), "głośniki w sypialni"),
            RelaySensorEntry.Create(9, new Relays.Tasmota("192.168.71.31", true), "Cambridge Audio DAC"),
            RelaySensorEntry.Create(10, new Relays.Tasmota("192.168.71.35", true), "lampki choinkowe"),
            RelaySensorEntry.Create(11, new Relays.Shelly("192.168.71.42", relayNumber: 0), "lampka na schodach w salonie")
        }.ToDictionary(x => x.Id, x => x);

        public static readonly Dictionary<int, IRelaySensorEntry<Sensors.IPowerMeter>> PowerMeters = new IRelaySensorEntry<Sensors.IPowerMeter>[]
        {
            RelaySensorEntry.Create(0, new Sensors.TasmotaPowerMeter("192.168.71.32"), "pralka"),
            RelaySensorEntry.Create(1, new Sensors.ShellyPowerMeter("192.168.71.38"), "mata grzejna prawa"),
            RelaySensorEntry.Create(2, new Sensors.ShellyPowerMeter("192.168.71.37"), "mata grzejna lewa"),
            RelaySensorEntry.Create(3, new Sensors.TasmotaPowerMeter("192.168.71.31"), "Cambridge Audio DAC"),
        }.ToDictionary(x => x.Id, x => x);

        public static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(30);

        private static readonly int[] BasicRange = { 0, 1, 2, 11 };

        public static readonly Scenario[] Scenarios =
        {
            new Scenario(BasicRange, Array.Empty<int>()),
            new Scenario(BasicRange, new [] { 0, 2 }),
            new Scenario(BasicRange, new [] { 1, 2, 11 }, new Dictionary<int, int> { { 2, 38 } }),
            new Scenario(BasicRange, new [] { 2, 11 }, new Dictionary<int, int> { { 2, 20 } }),
            new Scenario(BasicRange, new [] { 2 }, new Dictionary<int, int> { { 2, 12 } }),
            new Scenario(BasicRange, new [] { 0, 1, 2, 11 }),
        };

        public static readonly AutoScenarioHandler[] AutoScenarios = 
        {
            // disable bed heating on weekends as "normal" heating is triggered on that days
            new AutoScenarioHandler(4, false, (new HashSet<DayOfWeek>(new [] { DayOfWeek.Saturday, DayOfWeek.Sunday } ), "7:00", false)),
            new AutoScenarioHandler(5, false, (new HashSet<DayOfWeek>(new [] { DayOfWeek.Saturday, DayOfWeek.Sunday } ), "7:00", false))
        };

        public static readonly HeartbeatenHandler[] Heartbeatings = 
        {
            new HeartbeatenHandler(HeartbeatTimeout, 3, 9),
            new HeartbeatenHandler(HeartbeatTimeout, 8),
            new HeartbeatenHandler(TimeSpan.Zero, 6) // we do not really use "heartbeat" feature here
        };

        public static readonly IInfraredReceiverSender[] Infrareds = new[]
        {
            new TasmotaInfrared("192.168.71.41"),
        };

        public static readonly CultureInfo BotCommunicationCultureInfo = new CultureInfo("pl-PL");
    }
}