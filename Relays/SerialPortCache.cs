using System;
using System.Collections.Generic;
using System.IO;

namespace MieszkanieOswieceniaBot.Relays
{
	public sealed class SerialPortCache
	{
		public SerialPortCache()
		{
			ports = new Dictionary<string, FileStream>();
			sync = new object();
		}

		public FileStream GetPort(string name)
        {
			lock (sync)
			{
				if (ports.TryGetValue(name, out var port))
				{
					return port;
				}

				var options = new FileStreamOptions { BufferSize = 0, Access = FileAccess.ReadWrite, Mode = FileMode.Open };
				port = File.Open(name, options);
				ports.Add(name, port);
				return port;
			}
		}

		private readonly Dictionary<string, FileStream> ports;
		private readonly object sync;
	}
}

