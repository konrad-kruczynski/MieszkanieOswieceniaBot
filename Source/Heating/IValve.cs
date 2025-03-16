using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Heating;

public interface IValve : IElement
{
    decimal WarmTemperature { get; }
    decimal ColdTemperature { get; }
    Task<bool> Boost(bool enable);
    Task<(bool, decimal)> GetCurrentTemperature();
    Task<bool> SetTemperature(decimal temperature);
    
    Task<bool> IElement.Activate()
    {
        return SetTemperature(WarmTemperature);
    }

    Task<bool> IElement.Deactivate()
    {
        return SetTemperature(ColdTemperature);
    }
}