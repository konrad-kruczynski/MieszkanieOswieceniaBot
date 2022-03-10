using System;

namespace MieszkanieOswieceniaBot
{
    public sealed class LogEntry
    {
        public LogEntry(string text, DateTime date)
        {
            Text = text;
            Date = date;
        }

        public string Text { get; private set; }
        public DateTime Date { get; private set; }
    }
}

