using System;
using System.Text;
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
            var totalDays = (DateTime.Now - startTime).TotalDays;
            builder.AppendFormat("Jestem uruchomiony od {0:D}, czyli przez {1:0.0} dnia.", startTime, totalDays);
            builder.AppendLine();
            builder.AppendLine($"Obsłużyłem w tym czasie {messageCounter} wiadomości.");
            builder.AppendFormat("To daje średnio ~{0:0.0} wiadomości dziennie.", messageCounter / totalDays);
            builder.AppendLine();
            builder.AppendFormat("Rozmiar bazy danych: {0}.", ByteSize.FromBytes(TemperatureDatabase.Instance.FileSize));
            builder.AppendLine();
            builder.AppendFormat("Liczba próbek: {0}.", TemperatureDatabase.Instance.GetSampleCount());
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
