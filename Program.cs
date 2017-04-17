using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Threading;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            AsyncPump.Run(async () =>
            {
                var controller = new Controller();
                controller.Start();
                await Task.Delay(Timeout.InfiniteTimeSpan);
            });
        }
    }
}
