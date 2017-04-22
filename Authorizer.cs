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
            users = new HashSet<int>();
            LoadUsers();
        }

        public bool IsAuthorized(int userId)
        {
            return configuration.IsAdmin(userId) || ListUsers().Contains(userId);
        }

        public void AddUser(int userId)
        {
            users.Add(userId);
            Write();
        }

        public void RemoveUser(int userId)
        {
            users.Remove(userId);
            Write();
        }

        public IEnumerable<int> ListUsers()
        {
            return users;
        }

        private void LoadUsers()
        {
            if(!File.Exists(UsersFile))
            {
                Write();
            }
            foreach(var user in File.ReadAllLines(UsersFile).Select(x => int.Parse(x)))
            {
                users.Add(user);
            }
        }

        private void Write()
        {
            File.WriteAllLines(UsersFile, users.Select(x => x.ToString()).ToArray());
        }

        private readonly Configuration configuration;
        private readonly HashSet<int> users;
        private const string UsersFile = "users.txt";
    }
}
