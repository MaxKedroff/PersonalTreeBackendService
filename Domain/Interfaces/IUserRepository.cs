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

        Task<(List<User> Users, int TotalCount)> GetUsersPagedAsync(
        int page,
        int pageSize,
        string sortBy = null,
        string sortOrder = "asc",
        string positionFilter = null,
        string departmentFilter = null);

        Task<User> GetUsersByIdAsync(Guid UserId);
        Task<List<User>> GetSearchResultAsync(string criteria, string searchString, int queryAmount);
        Task<List<User>> GetUsersWithHierarchyAsync();
        Task<User> GetCeoAsync();
        Task<User> GetUserByLoginAsync(string login);
        Task UpdateUserAsync(User user);

    }
}
