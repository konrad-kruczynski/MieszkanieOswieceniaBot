using System;
using LiteDB;

namespace MieszkanieOswieceniaBot
{
    public class DatabaseChatId
    {
        [BsonId]
        public long Id { get; set; }
    }
}
