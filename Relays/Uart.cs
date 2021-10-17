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

        public bool State
        {
            get
            {
                return WriteAndReadMessage(CommandBase + relayOffset + StateOffset) == TurnOnOffset;
            }

            set
            {
                WriteMessage(CommandBase + relayOffset + (value ? TurnOnOffset : TurnOffOffset));
            }
        }

        public bool Toggle()
        {
            var newState = !State;
            State = newState;
            return newState;
        }


        private readonly string deviceName;
        private readonly byte relayOffset;

        private const int CommandBase = 48;
        private const int StateOffset = 2;
        private const int TurnOnOffset = 1;
        private const int TurnOffOffset = 0;

        private static void WithSerialPort(string name, Action<SerialPort> action)
        {
            using(var serialPort = new SerialPort(name, 9600))
            {
                try
                {
                    serialPort.Open();
                    CircularLogger.Instance.Log("Serial port '{0}' opened.", name);
                    action(serialPort);
                }
                catch(IOException)
                {
                    CircularLogger.Instance.Log("Could not open port '{0}', did nothing.", name);
                }
            }
        }

        private void WriteMessage(int message)
        {
            WithSerialPort(deviceName, serialPort =>
            {
                serialPort.Write(new[] { checked((byte)message) }, 0, 1);
            });
        }

        private byte WriteAndReadMessage(int message)
        {
            var result = new byte[1];

            WithSerialPort(deviceName, serialPort =>
            {
                serialPort.Write(new[] { checked((byte)message) }, 0, 1);
                serialPort.Read(result, 0, 1);
            });

            return result[0];
        }
    }
}
