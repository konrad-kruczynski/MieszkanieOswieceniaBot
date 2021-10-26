using System;
using System.IO;
using System.IO.Ports;

namespace MieszkanieOswieceniaBot.Relays
{
    public sealed class Uart : IRelay
    {
        public Uart(string deviceName, int relayNumber)
        {
            this.deviceName = deviceName;
            relayOffset = (byte)(relayNumber * 3);
        }

        public bool TryGetState(out bool state)
        {
            if(cachedState.HasValue)
            {
                state = cachedState.Value;
                return true;
            }

            var success = WriteAndReadMessage(CommandBase + relayOffset + StateOffset, out var result);
            if (success)
            {
                cachedState = result == CommandBase + TurnOnOffset;
                state = cachedState.Value;
                return true;
            }

            state = false;
            return false;
        }

        public bool TrySetState(bool state)
        {
            if(cachedState.HasValue && cachedState.Value == state)
            {
                return true;
            }

            var success = WriteMessage(CommandBase + relayOffset + (state ? TurnOnOffset : TurnOffOffset));

            if (success)
            {
                cachedState = state;
                return true;
            }

            return false;
        }

        public bool TryToggle(out bool currentState)
        {
            currentState = false;
            return TryGetState(out var state) && TrySetState(currentState = !state);
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
            using(var serialPort = new SerialPort(name, 9600))
            {
                try
                {
                    serialPort.Open();
                    action(serialPort);
                    return true;
                }
                catch(IOException)
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
