using System;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot
{
    public sealed class Entry<T> : IEntry<T>
    {
        public Entry(int id, T element, string friendlyName)
        {
            Id = id;
            Element = element;
            FriendlyName = friendlyName;
        }

        public int Id { get; private set; }
        public T Element { get; private set; }
        public string FriendlyName { get; private set; }
    }

    public static class Entry
    {
        public static Entry<T> Create<T>(int id, T relaySensor, string friendlyName)
        {
            return new Entry<T>(id, relaySensor, friendlyName);
        }
    }
}
