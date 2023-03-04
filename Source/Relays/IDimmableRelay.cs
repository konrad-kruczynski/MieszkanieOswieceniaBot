using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Relays
{
	public interface IDimmableRelay : IRelay
	{
		Task<bool> DimToAsync(int value);
		Task<(bool Success, int Value)> GetDimValueAsync();
	}
}

