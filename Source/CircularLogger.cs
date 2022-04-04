using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MieszkanieOswieceniaBot
{
    public class CircularLogger
    {
        static CircularLogger()
        {
            Instance = new CircularLogger(1000);
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

        public IEnumerable<string> GetEntriesAsHtmlStrings()
        {
            lock(sync)
            {
                var grouped = entries.GroupBy(x => x.Text).OrderBy(x => x.Max(y => y.Date));
                var result = new List<string>();

                foreach (var group in grouped)
                {
                    var orderedGroup = group.OrderBy(x => x.Date);
                    if (group.Count() == 1)
                    {
                        result.Add(string.Format("<pre>{0:d MMM HH:mm:ss} {1}</pre>",
                            orderedGroup.First().Date,
                            WebUtility.HtmlEncode(TrimIfNecessary(group.Key))));
                    }
                    else
                    {
                        result.Add(string.Format("<pre>{0:d MMM HH:mm:ss} (+{2} in last {3} hours) {1}</pre>",
                            orderedGroup.Last().Date,
                            WebUtility.HtmlEncode(TrimIfNecessary(group.Key)),
                            group.Count() - 1,
                            (orderedGroup.Last().Date - orderedGroup.First().Date).TotalHours));
                    }
                }

                return result;
            }
        }

        private string TrimIfNecessary(string message)
        {
            if (message.Length > 2000)
            {
                return message.Substring(0, 2000) + "... (trimmed)";
            }

            return message;
        }

        private readonly Queue<LogEntry> entries;
        private readonly int capacity;
        private readonly object sync;
    }
}

