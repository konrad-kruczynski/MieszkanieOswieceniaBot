using System;
using System.Collections.Generic;
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
            admins = new HashSet<long>(File.ReadAllLines("admins.txt").Select(x => long.Parse(x)));
        }

        public string GetApiKey()
        {
            var keys = File.ReadAllLines("apiKey.txt");
            return keys[0];
        }

        public bool IsAdmin(long id)
        {
            return admins.Contains(id);
        }

        public IEnumerable<long> ListAdmins()
        {
            return admins;
        }

        private readonly HashSet<long> admins;
    }
}
