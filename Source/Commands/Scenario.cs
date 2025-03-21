﻿using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
	public class Scenario : ITextCommand
	{
        public Scenario(int scenarioNumber)
        {
            this.scenarioNumber = scenarioNumber;
        }

        public async Task<string> ExecuteAsync(TextCommandParameters parameters)
        {
            parameters.ExpectNoOtherParameters();
            var scenario = Globals.Scenarios[scenarioNumber];
            if (!await scenario.TryApplyAsync())
            {
                return "Nie udało się w całości wykonać scenariusza";
            }

            return $"Scenariusz {scenarioNumber} uaktywniony ({scenario.GetFriendlyDescription(Globals.Relays)}).";
        }

        private readonly int scenarioNumber;
    }
}

