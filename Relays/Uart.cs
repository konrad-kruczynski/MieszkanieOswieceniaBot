using System;
using System.IO;
using System.Threading;
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
            if(cachedState.HasValue && cachedState.Value == state)
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
            var currentState = await TryGetStateAsync();
            if (!currentState.Success)
            {
                return currentState;
            }

            var settingSuccess = await TrySetStateAsync(!currentState.State);
            return (settingSuccess, !currentState.State);
        }

        private static bool WithSerialPort(string name, Action<FileStream> action)
        {
            try
            {
                lock (PortCache)
                {
                    var serialPort = PortCache.GetPort(name);
                    if (!Task.Run(() => action(serialPort)).Wait(TimeSpan.FromSeconds(3)))
                    {
                        CircularLogger.Instance.Log("Timeout during open/read/write port '{0}', did nothing.", name);
                        return false;
                    }

                    return true;
                }
            }
            catch (IOException)
            {
                CircularLogger.Instance.Log("Could not open/read/write port '{0}', did nothing.", name);
                return false;
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
            var resultAsInt = -1;

            var success = WithSerialPort(deviceName, serialPort =>
            {
                serialPort.WriteByte(checked((byte)message));
                resultAsInt = serialPort.ReadByte();
            });

            if (resultAsInt != -1)
            {
                result = (byte)resultAsInt;
                return true;
            }
            else
            {
                result = 0;
                return false;
            }
        }

        private bool? cachedState;

        private readonly string deviceName;
        private readonly byte relayOffset;

        private const int CommandBase = 48;
        private const int StateOffset = 2;
        private const int TurnOnOffset = 1;
        private const int TurnOffOffset = 0;

        private static readonly SerialPortCache PortCache = new SerialPortCache();
    }
}
