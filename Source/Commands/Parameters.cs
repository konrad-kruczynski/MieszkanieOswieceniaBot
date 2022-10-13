using System;
using System.Collections.Generic;

namespace MieszkanieOswieceniaBot.Commands
{ 
	public class TextCommandParameters
	{
		public TextCommandParameters(string commandName, IReadOnlyList<string> parameters)
		{
			this.parameters = parameters;
			CommandName = commandName;
		}
		
		public string CommandName { get; init; }
		public bool IsAdmin { get; init; }
		public int Count => parameters.Count;

		public bool TryTakeString(out string value)
		{
			if (counter >= parameters.Count)
			{
				value = null;
				return false;
			}

			value = parameters[counter++];
			return true;
		}

		public string TakeString()
        {
			if (!TryTakeString(out var value))
            {
				throw new ParameterException(ParameterExceptionType.NotEnoughParameters);
            }

			return value;
        }

		public bool TryTakeInteger(out int value)
        {
			if (!TryTakeString(out var integerAsString) || !int.TryParse(integerAsString, out value))
            {
				value = default;
				return false;
            }

			return true;
        }

		public int TakeInteger()
        {
			if (!TryTakeString(out var integerAsString))
            {
				throw new ParameterException(ParameterExceptionType.NotEnoughParameters);
            }

			if (!int.TryParse(integerAsString, out var value))
            {
				throw new ParameterException(ParameterExceptionType.ConversionError) { Position = counter };
            }

			return value;
        }

		public bool TryTakeEnum<T>(out T value) where T : struct
		{
			if (!TryTakeString(out var enumAsString) || !Enum.TryParse<T>(enumAsString, true, out value))
			{
				value = default;
				return false;
			}

			return true;
		}

		public T TakeEnum<T>() where T : struct
        {
			if (!TryTakeString(out var enumAsString))
			{
				throw new ParameterException(ParameterExceptionType.NotEnoughParameters);
			}

			if (!Enum.TryParse<T>(enumAsString, true, out var value))
			{
				throw new ParameterException(ParameterExceptionType.OutOfRangeError) { Position = counter };
			}

			return value;
		}

		public void ExpectNoOtherParameters()
        {
			if(counter != parameters.Count)
            {
				throw new ParameterException(ParameterExceptionType.TooMuchParameters);
            }
        }

		private readonly IReadOnlyList<string> parameters;
		private int counter;
	}
}

