﻿using System;
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
            var totalDays = (DateTime.Now - startTime).TotalDays;
            builder.AppendFormat("Jestem uruchomiony od {0:D}, czyli przez {1:0.0} dnia.", startTime, totalDays);
            builder.AppendLine();
            builder.AppendLine($"Obsłużyłem w tym czasie {messageCounter} wiadomości.");
            builder.AppendFormat("To daje średnio ~{0:0.0} wiadomości dziennie.", messageCounter / totalDays);
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