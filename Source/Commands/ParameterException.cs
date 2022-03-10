using System;

namespace MieszkanieOswieceniaBot.Commands
{
	public sealed class ParameterException : Exception
	{
		public ParameterException(ParameterExceptionType type)
		{
			Type = type;
		}

		public ParameterExceptionType Type { get; init; }
		public int? Position { get; init; }

        public override string ToString()
        {
			return $"{nameof(ParameterException)}: {Type}";
        }
    }
}

