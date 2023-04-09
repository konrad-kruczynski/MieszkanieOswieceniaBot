using System;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot
{
    public sealed class RelaySensorEntry<T> : IRelaySensorEntry<T>
    {
        public RelaySensorEntry(int id, T relaySensor, string friendlyName)
        {
            Id = id;
            RelaySensor = relaySensor;
            FriendlyName = friendlyName;
        }

        public int Id { get; private set; }
        public T RelaySensor { get; private set; }
        public string FriendlyName { get; private set; }
    }

    public static class RelaySensorEntry
    {
        public static RelaySensorEntry<T> Create<T>(int id, T relaySensor, string friendlyName)
        {
            return new RelaySensorEntry<T>(id, relaySensor, friendlyName);
        }
    }
}
