using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.OtherDevices;

public interface IInfraredReceiverSender
{
    Task<bool> SendInfrared(uint data, uint dataLsb);
}