using System;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Relays
{
    public sealed class Uart : IRelay
    {
        public Uart(string deviceName, int relayNumber)
        {
            this.deviceName = deviceName;
            relayOffset = (byte)(relayNumber * 3);
        }

        public Task<(bool Success, bool State)> TryGetStateAsync()
        {
            if (cachedState.HasValue)
            {
                return Task.FromResult((true, cachedState.Value));
            }

            var success = WriteAndReadMessage(CommandBase + relayOffset + StateOffset, out var result);
            if (success)
            {
                cachedState = result == CommandBase + TurnOnOffset;
                return Task.FromResult((true, cachedState.Value));
            }

            return Task.FromResult((false, false));
        }

        public Task<bool> TrySetStateAsync(bool state)
        {
            if (cachedState.HasValue && cachedState.Value == state)
            {
                return Task.FromResult(true);
            }

            var success = WriteMessage(CommandBase + relayOffset + (state ? TurnOnOffset : TurnOffOffset));

            if (success)
            {
                cachedState = state;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public async Task<(bool Success, bool CurrentState)> TryToggleAsync()
        {
            var (success, state) = await TryGetStateAsync();
            if (!success)
            {
                return (false, false);
            }

            success = await TrySetStateAsync(!state);

            if (!success)
            {
                return (false, false);
            }

            return (true, !state);
        }

        private bool? cachedState;

        private readonly string deviceName;
        private readonly byte relayOffset;

        private const int CommandBase = 48;
        private const int StateOffset = 2;
        private const int TurnOnOffset = 1;
        private const int TurnOffOffset = 0;

        private static bool WithSerialPort(string name, Action<SerialPort> action)
        {
            using (var serialPort = new SerialPort(name, 9600))
            {
                try
                {
                    serialPort.Open();
                    action(serialPort);
                    return true;
                }
                catch (IOException)
                {
                    CircularLogger.Instance.Log("Could not open port '{0}', did nothing.", name);
                    return false;
                }
            }
        }

        private bool WriteMessage(int message)
        {
            return WithSerialPort(deviceName, serialPort =>
            {
                serialPort.Write(new[] { checked((byte)message) }, 0, 1);
            });
        }

        private bool WriteAndReadMessage(int message, out byte result)
        {
            var resultArray = new byte[1];

            var success = WithSerialPort(deviceName, serialPort =>
            {
                serialPort.Write(new[] { checked((byte)message) }, 0, 1);
                serialPort.Read(resultArray, 0, 1);
            });

            result = resultArray[0];
            return success;
        }
    }
}