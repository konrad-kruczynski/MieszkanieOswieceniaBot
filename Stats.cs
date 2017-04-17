using System;
using System.Text;

namespace MieszkanieOswieceniaBot
{
    public sealed class Stats
    {
        public Stats()
        {
            startTime = DateTime.Now;
        }

        public string GetStats()
        {
            var builder = new StringBuilder();
            builder.AppendFormat("Jestem uruchomiony od {0:D}, czyli przez {1:g}.", startTime, DateTime.Now - startTime);
            builder.AppendLine();
            builder.AppendFormat($"Obsłużyłem w tym czasie {messageCounter} wiadomości.");
            return builder.ToString();
        }

        public void IncrementMessageCounter()
        {
            messageCounter++;
        }

        private int messageCounter;
        private readonly DateTime startTime;
    }
}
