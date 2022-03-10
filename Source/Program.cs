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

                try
                {
                    await controller.Run();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            });
        }
    }
}
