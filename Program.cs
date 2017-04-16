using System;
using System.Threading;
using Telegram.Bot;

namespace MieszkanieOswieceniaBot
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var controller = new Controller();
            controller.Start();
            new ManualResetEvent(false).WaitOne();
        }
    }
}
