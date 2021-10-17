using System;
using MieszkanieOswieceniaBot.Relays;

namespace MieszkanieOswieceniaBot
{
    public sealed class RelayEntry
    {
        public RelayEntry(int id, IRelay relay, string friendlyName)
        {
            Id = id;
            Relay = relay;
            FriendlyName = friendlyName;
        }

        public int Id { get; private set; }
        public IRelay Relay { get; private set; }
        public string FriendlyName { get; private set; }
    }
}
