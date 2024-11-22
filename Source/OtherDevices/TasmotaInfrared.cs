using System;
using System.Threading.Tasks;
using Flurl.Http;
using MieszkanieOswieceniaBot.Base;

namespace MieszkanieOswieceniaBot.OtherDevices;

public class TasmotaInfrared : HttpBasedSomething, IInfraredReceiverSender
{
    public TasmotaInfrared(string hostname, TimeSpan timeout = default) : base(hostname, timeout)
    {
        
    }

    public async Task<bool> SendInfrared(uint data, uint dataLsb)
    {
        var command = "IrSend {\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\""
                      + string.Format("0x{0:X}", data) 
                      + "\",\"DataLSB\":\"" 
                      + string.Format("0x{0:X}", dataLsb) 
                      + "\",\"Repeat\":0}";
        
        var result = await FlurlClient.Request($"cm?cmnd={command}").GetStringAsync().ConfigureAwait(false);

        return result == "{\"IRSend\":\"Done\"}";
    }
}