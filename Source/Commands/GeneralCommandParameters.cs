using System;
using System.Collections.Generic;

namespace MieszkanieOswieceniaBot.Commands
{
	public class GeneralCommandParameters : TextCommandParameters
	{
        public GeneralCommandParameters(long chatId, long senderId, string commandName, IReadOnlyList<string> parameters)
            : base(commandName, parameters)
        {
            ChatId = chatId;
            SenderId = senderId;
        }

        public long ChatId { get; init; }
        public long SenderId { get; init; }
    }
}

