using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Heating;

public interface IElement
{
    Task<bool> Activate();
    Task<bool> Deactivate();
} 