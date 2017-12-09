using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MieszkanieOswieceniaBot
{
    public class PekaDb
    {
        public static PekaDb Instance { get; private set; }

        static PekaDb()
        {
            Instance = new PekaDb();
        }

        public IEnumerable<(string, string, string)> GetData()
        {
            if(!File.Exists(DatabaseFile))
            {
                return Enumerable.Empty<(string, string, string)>();
            }
            return File.ReadLines(DatabaseFile).Select(x => x.Split('|')).Select(x => (x[0], x[1], x[2]));
        }

        private PekaDb()
        {
        }

        private const string DatabaseFile = "peka.txt";
    }
}
