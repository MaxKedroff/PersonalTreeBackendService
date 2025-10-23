using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ActiveDirectory
{
    public interface ILdapService
    {
        Task<List<User>> GetUsersFromActiveDirectoryAsync();
        Task<User> GetUserBySamAccountNameAsync(string samAccountName);
    }
}
