using System;
using System.Collections.Generic;
using System.Linq;

namespace MieszkanieOswieceniaBot
{
	internal static class Globals
	{
        public static readonly Dictionary<int, RelayEntry> Relays = new[]
        {
            new RelayEntry(0, new Relays.Uart("/dev/ttyUSB1", 0), "lampa doniczka"),
            new RelayEntry(1, new Relays.Uart("/dev/ttyUSB1", 1), "lampa stojąca"),
            new RelayEntry(2, new Relays.Shelly("192.168.71.33"), "lampa przy kanapie"),
            new RelayEntry(3, new Relays.Uart("/dev/ttyUSB0", 0), "głośniki"),
            new RelayEntry(4, new Relays.Tasmota("192.168.71.31"), "mata grzejna Kota"),
            new RelayEntry(5, new Relays.Tasmota("192.168.71.32"), "mata grzejna Kocicy"),
            new RelayEntry(6, new Relays.Shelly("192.168.71.34"), "lampa zewnętrzna"),
            new RelayEntry(7, new Relays.Tasmota("192.168.71.35"), "oświetlenie akwarium"),
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
            new Scenario(BasicRange, new [] { 0, 1, 2}),
            new Scenario(BasicRange, new [] { 0, 2 }),
        };

        public static readonly AutoScenarioHandler[] AutoScenarios = new[]
        {
            new AutoScenarioHandler(7, true, ("13:15", true), ("13:17", false), ("13:19", true), ("13:21", false), ("13:23", true), ("13:25", false))
        };
    }
}