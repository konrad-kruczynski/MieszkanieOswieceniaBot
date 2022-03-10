using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Commands
{
    public sealed class RosyCreekCooperative : ITextCommand
    {
        public Task<string> ExecuteAsync(Parameters parameters)
        {
            if (parameters.Count == 0)
            {
                Database.Instance.AddHouseCooperativeChatId(parameters.ChatId);
                return Task.FromResult("Dodano");
            }

            var action = parameters.TakeEnum<RosyCreekAction>();

            switch (action)
            {
                case RosyCreekAction.Reset:
                    Database.Instance.NewestKnownRosyCreekNewsDate = DateTime.MinValue;
                    return Task.FromResult("Zresetowano");
                default:
                    throw new ParameterException(ParameterExceptionType.OutOfRangeError);
            }
        }
    }

    public enum RosyCreekAction
    {
        Reset
    }
}

