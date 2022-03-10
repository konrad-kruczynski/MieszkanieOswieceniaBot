using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Handlers
{
    public interface IHandler
    {
        Task RefreshAsync();
    }
}