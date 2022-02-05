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
    }
}