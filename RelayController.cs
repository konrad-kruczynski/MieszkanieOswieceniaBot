using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using Flurl.Http;

namespace MieszkanieOswieceniaBot
{
    public sealed class RelayController
    {
        public RelayController()
        {
            relayStateCache = new bool[4];
            serialPort1 = TryGetSerialPort("/dev/ttyUSB0");
            serialPort2 = TryGetSerialPort("/dev/ttyUSB1");

            // get the last state from the database
            var statesToSet = new bool[relayStateCache.Length];
            var lastState = Database.Instance.GetSamples<StateSample>(DateTime.Now.AddDays(-7), DateTime.Now)
                                    .OrderBy(x => x.Date).LastOrDefault();
            if(lastState != null)
            {
                CircularLogger.Instance.Log("Setting last state from the DB: {0}.", lastState);
                statesToSet = lastState.GetStateArray();
            }
            for(var i = 0; i < relayStateCache.Length; i++)
            {
                relayStateCache[i] = statesToSet[i];
                TrySetStatePhysical(LogicalToPhysicalRelayNo[i], statesToSet[i]);
            }
        }

        public bool GetState(int relayNo)
        {
            if (relayNo == 2)
            {
                var jsonState = "http://192.168.71.33/relay/0".GetJsonAsync().GetAwaiter().GetResult();
                return jsonState.ison;
            }

            return relayStateCache[relayNo];
        }

        public void SetState(int relayNo, bool state)
        {
            if (relayNo == 2)
            {
                var stateAsText = state ? "on" : "off";
                $"http://192.168.71.33/relay/0?turn={stateAsText}".GetAsync().GetAwaiter().GetResult();
                return;
            }

            if(state == relayStateCache[relayNo])
            {
                return;
            }
            relayStateCache[relayNo] = state;
            var physicalNo = LogicalToPhysicalRelayNo[relayNo];
            CircularLogger.Instance.Log("Setting relay {0} (={2} physical) {1}.", relayNo, state, physicalNo);
            TrySetStatePhysical(physicalNo, state);
        }

        public bool[] GetStateArray()
        {
            var result = relayStateCache.ToArray();
            result[2] = GetState(2);
            return result;
        }

        public void SetStateFromArray(bool[] state)
        {
            for(var i = 0; i < relayStateCache.Length; i++)
            {
                SetState(i, state[i]); 
            }
        }

        public static int RelayCount
        {
            get
            {
                return LogicalToPhysicalRelayNo.Count;
            }
        }

        private void TrySetStatePhysical(int physicalRelayNo, bool state)
        {
            var relayOffset = physicalRelayNo % 2 == 0 ? 0 : 3;
            var serialPort = physicalRelayNo < 2 ? serialPort1 : serialPort2;
            serialPort.WriteByteOrDoNothing((byte)(CommandBase + relayOffset + (state ? TurnOnOffset : TurnOffOffset)));
        }

        private SerialPort TryGetSerialPort(string name)
        {
            var result = new SerialPort(name, 9600);
            try
            {
                result.Open();
            }
            catch(IOException)
            {
                CircularLogger.Instance.Log("Could not open port '{0}', going with dummy mode.", name);
                return null;
            }
            CircularLogger.Instance.Log("Serial port '{0}' opened.", name);
            return result;
        }

        private readonly bool[] relayStateCache;
        private readonly SerialPort serialPort1;
        private readonly SerialPort serialPort2;

        private const byte StateOffset = 2;
        private const byte TurnOnOffset = 1;
        private const byte TurnOffOffset = 0;
        private const byte CommandBase = 48;

        private static readonly Dictionary<int, int> LogicalToPhysicalRelayNo = new Dictionary<int, int>
        {
            // W komentarzu fizyczne podłączenia
            { 0, 2 }, // 1 = pusty
            { 1, 3 }, // 2 = doniczka
            { 2, 1 }, // 3 = lampa stojąca
            { 3, 0 }  // 0 = głośniki
        };
    }
}
