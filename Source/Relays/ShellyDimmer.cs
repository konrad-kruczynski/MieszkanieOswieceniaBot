using System;
namespace MieszkanieOswieceniaBot.Relays
{
	public class ShellyDimmer : Shelly
	{
		public ShellyDimmer(string hostname) : base(hostname, "light")
		{
		}
	}
}

