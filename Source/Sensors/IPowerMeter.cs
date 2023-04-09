using System;
using System.Threading.Tasks;

namespace MieszkanieOswieceniaBot.Sensors
{
	public interface IPowerMeter
	{
		Task<(decimal, bool)> TryGetCurrentUsageAsync();
	}
}

