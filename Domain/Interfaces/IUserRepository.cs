using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<List<User>> GetUsersAsync();

        Task<User> GetUsersByIdAsync(Guid UserId);

        Task<List<User>> GetSearchResultAsync(string criteria, string searchString, int queryAmount);
    }
}
