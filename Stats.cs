using System;
using System.Globalization;
using System.Text;
using Humanizer;
using Humanizer.Bytes;

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
            var runningTime = (DateTime.Now - startTime);
            builder.AppendFormat("Jestem uruchomiony od {0:D}, czyli przez {1}.", startTime, runningTime.Humanize(culture: new CultureInfo("pl-PL")));
            builder.AppendLine();
            builder.AppendLine($"Obsłużyłem w tym czasie {messageCounter} wiadomości.");
            builder.AppendFormat("To daje średnio ~{0:0.0} wiadomości dziennie.", messageCounter / runningTime.TotalDays);
            builder.AppendLine();
            builder.AppendFormat("Rozmiar bazy danych: {0}.", ByteSize.FromBytes(Database.Instance.FileSize).Humanize("#.##"));
            builder.AppendLine();
            builder.AppendFormat("Liczba próbek temperatury: {0}.", Database.Instance.GetSampleCount<TemperatureSample>());
            builder.AppendLine();
            builder.AppendFormat("Liczba próbek stanu: {0}.", Database.Instance.GetSampleCount<RelaySample>());
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
