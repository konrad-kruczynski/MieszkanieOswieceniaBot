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
        }

        public bool IsAuthorized(int userId)
        {
            return configuration.IsAdmin(userId) || ListUsers().Contains(userId);
        }

        public void AddUser(int userId)
        {
            Write(ListUsers().Concat(new[] { userId }));
        }

        public void RemoveUser(int userId)
        {
            Write(ListUsers().Where(x => x != userId));
        }

        public IEnumerable<int> ListUsers()
        {
            if(!File.Exists(UsersFile))
            {
                Write(new int[0]);
            }
            return File.ReadAllLines(UsersFile).Select(x => int.Parse(x));
        }

        private void Write(IEnumerable<int> users)
        {
            File.WriteAllLines(UsersFile, users.Select(x => x.ToString()).Distinct().ToArray());
        }

        private readonly Configuration configuration;
        private const string UsersFile = "users.txt";
    }
}
