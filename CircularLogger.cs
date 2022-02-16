using System;
using System.Collections.Generic;
using System.Linq;

namespace MieszkanieOswieceniaBot
{
    public class CircularLogger
    {
        static CircularLogger()
        {
            Instance = new CircularLogger(40);
        }

        public static CircularLogger Instance { get; private set; }

        public CircularLogger(int capacity)
        {
            this.capacity = capacity;
            entries = new Queue<LogEntry>(capacity);
            sync = new object();
        }

        public void Log(string formatString, params object[] arguments)
        {
            Log(string.Format(formatString, arguments));
        }

        public void Log(string message)
        {
            lock(sync)
            {
                var entry = new LogEntry(message, DateTime.Now);
                entries.Enqueue(entry);
                Console.WriteLine("{0:G} {1}", entry.Date, entry.Text);
                if(entries.Count > capacity)
                {
                    entries.Dequeue();
                }
            }
        }

        public IEnumerable<LogEntry> GetEntries()
        {
            lock(sync)
            {
                return entries.Select(x => x);
            }
        }

        public IEnumerable<string> GetEntriesAsStrings()
        {
            lock(sync)
            {
                return entries.Select(x => string.Format("<pre>{0:d MMM HH:mm:ss} {1}</pre>", x.Date, x.Text))
                    .ToArray();
            }
        }

        private readonly Queue<LogEntry> entries;
        private readonly int capacity;
        private readonly object sync;
    }
}

