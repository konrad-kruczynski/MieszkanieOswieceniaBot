using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MieszkanieOswieceniaBot
{
    public sealed class Authorizer
    {
        public Authorizer()
        {
            configuration = Configuration.Instance;
            users = new HashSet<long>();
            LoadUsers();
        }

        public bool IsAuthorized(long userId)
        {
            return configuration.IsAdmin(userId) || ListUsers().Contains(userId);
        }

        public void AddUser(long userId)
        {
            users.Add(userId);
            Write();
        }

        public void RemoveUser(int userId)
        {
            users.Remove(userId);
            Write();
        }

        public IEnumerable<long> ListUsers()
        {
            return users;
        }

        private void LoadUsers()
        {
            if(!File.Exists(UsersFile))
            {
                Write();
            }
            foreach(var user in File.ReadAllLines(UsersFile).Select(x => long.Parse(x)))
            {
                users.Add(user);
            }
        }

        private void Write()
        {
            File.WriteAllLines(UsersFile, users.Select(x => x.ToString()).ToArray());
        }

        private readonly Configuration configuration;
        private readonly HashSet<long> users;
        private const string UsersFile = "users.txt";
    }
}
