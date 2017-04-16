using System;
using System.IO.Ports;
using System.Threading;

namespace MieszkanieOswieceniaBot
{
    public static class SerialPortHelpers
    {
        public static void WriteByteOrDoNothing(this SerialPort port, byte b)
        {
            if(port == null)
            {
                return;
            }
            port.Write(new [] { b }, 0, 1);
        }

        public static byte ReadByte(this SerialPort port)
        {
            var result = new byte[1];
            var read = port.Read(result, 0, 1);
            if(read != 1)
            {
                throw new InvalidOperationException();
            }
            return result[0];
        }
    }
}

