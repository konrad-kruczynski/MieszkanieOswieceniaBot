﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MieszkanieOswieceniaBot.Commands;
using Similarity;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot
{
	public sealed class CommandRegister
	{
		public CommandRegister(ITelegramBotClient bot)
		{
			this.bot = bot;
			commands = new Dictionary<string, ICommand>();
			commandListCommand = new CommandList(this);
			authorizer = new Authorizer();
		}

		public void RegisterCommand(string name, ICommand command)
        {
			commands.Add(name, command);
        }

		public void RegisterCommandListCommandAs(string name)
        {
			commands.Add(name, commandListCommand);
        }

		public async Task HandleMessage(Telegram.Bot.Types.Message message)
        {
			var chatId = message.Chat.Id;
			var senderId = message.From.Id;
			if (!authorizer.IsAuthorized(senderId))
			{
				await bot.SendTextMessageAsync(chatId, "Brak dostępu.");
				CircularLogger.Instance.Log($"Unauthorized access from {message.From.FirstName} {message.From.LastName}.");
				return;
			}

			var commandText = message.Text.ToLower(); // TODO: what if no text
			var commandParts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			var commandName = commandParts[0];
			if (!commands.TryGetValue(commandName, out var command))
            {
				var mostSimilarCommandName = commands.Keys
					.Select(x => (x, StringSimilarity.Calculate(commandName, x)))
					.OrderByDescending(x => x.Item2).First();

				await bot.SendTextMessageAsync(chatId,
					$"Nie znalazłem komendy '{commandName}'. Czy chodziło Ci o '{mostSimilarCommandName.Item1}'?");
				CircularLogger.Instance.Log($"Unknown command '{commandName}' sent by {message.From.FirstName} {message.From.LastName}");

				return;
            }

			var privilegedAttribute = command.GetType().GetCustomAttribute<PrivilegedAttribute>();
			if (privilegedAttribute != null && !Configuration.Instance.IsAdmin(senderId))
			{
				await bot.SendTextMessageAsync(chatId, "Brak uprawnień - komenda dostępna tylko dla administratora.");
				CircularLogger.Instance.Log(
					$"Unauthorized (non-admin) attempt to execute '{commandName}' from {message.From.FirstName} {message.From.LastName}.");
				return;
			}

			var parameters = new Parameters(commandParts[0], commandParts.Skip(1).ToArray(), chatId, senderId);

			try
			{
				if (command is ITextCommand textCommand)
				{
					var response = await textCommand.ExecuteAsync(parameters);
					await bot.SendTextMessageAsync(chatId, response);
				}
				else if (command is IGeneralCommand nonTextCommand)
                {
					await nonTextCommand.ExecuteAsync(parameters);
                }

				parameters.ExpectNoOtherParameters();
			}
			catch(ParameterException exception)
            {
				var errorMessage = exception.Type switch
                {
                    ParameterExceptionType.NotEnoughParameters => "za mało parametrów dla komendy.",
                    ParameterExceptionType.TooMuchParameters => "zbyt dużo parametrów dla komendy.",
					ParameterExceptionType.ConversionError => $"zły typ parametru nr {exception.Position}, konwersja nieudana.",
					ParameterExceptionType.OutOfRangeError => $"wartość parametru nr {exception.Position} spoza dozwolonych",
                    _ => throw new NotImplementedException("Unknown parameter exception type")
                };

				await bot.SendTextMessageAsync(chatId, $"Błąd komendy: {errorMessage}");
            }
        }

		private readonly Dictionary<string, ICommand> commands;
		private readonly ITelegramBotClient bot;
		private readonly CommandList commandListCommand;
		private readonly Authorizer authorizer;

        private class CommandList : ITextCommand
        {
			public CommandList(CommandRegister parent)
			{
				this.parent = parent;
			}

			public Task<string> ExecuteAsync(Parameters parameters)
            {
				var builder = new StringBuilder();
				builder.AppendLine("Dostępne są następujące komendy:");

				foreach (var commandName in parent.commands.Keys)
                {
					builder.Append("- ");
					builder.AppendLine(commandName);
                }

				return Task.FromResult(builder.ToString());
            }

			private readonly CommandRegister parent;
        }
    }
}
