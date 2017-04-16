using System;
using System.IO;
using System.Linq;

namespace MieszkanieOswieceniaBot
{
    public class Configuration
    {
        static Configuration()
        {
            Instance = new Configuration();
        }

        public static Configuration Instance { get; private set; }

        private Configuration()
        {
            
        }

        public string GetApiKey()
        {
            var keys = File.ReadAllLines("apiKey.txt");
            return keys[0];
        }

        public bool IsAdmin(int id)
        {
            return File.ReadAllLines("admins.txt").Select(x => int.Parse(x)).Any(x => x == id);
        }
    }
}
