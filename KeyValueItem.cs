using System;
using LiteDB;

namespace MieszkanieOswieceniaBot
{
    public sealed class KeyValueItem
    {
        [BsonId]
        public string Key { get; set; }
        public byte[] Value { get; set; }
    }
}
