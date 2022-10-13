using System;
using System.Reflection;

namespace MieszkanieOswieceniaBot.Commands
{
	public static class CommandExtensions
	{
		public static bool IsPrivileged(this ICommand command)
		{
            var privilegedAttribute = command.GetType().GetCustomAttribute<PrivilegedAttribute>();
			return privilegedAttribute != null;
        }
	}
}

