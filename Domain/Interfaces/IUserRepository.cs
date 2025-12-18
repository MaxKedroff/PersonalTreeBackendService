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
        int page, int pageSize, string sortBy = null, string sortOrder = "asc",
        List<string> positionFilters = null, List<string> departmentFilters = null, string searchText = null);

        Task<User> GetUsersByIdAsync(Guid UserId);
        Task<List<User>> GetSearchResultAsync(string criteria, string searchString, int queryAmount);
        Task<List<User>> GetUsersWithHierarchyAsync();
        Task<List<User>> GetUsersWithHierarchyV2Async();

        Task<string> GetColorByTitleHierarchy(string title);

        Task<User> GetCeoAsync();
        Task<User> GetUserByLoginAsync(string login);
        Task UpdateUserAsync(User user);

        Task<List<Hierarchy>> GetHierarchiesList();

        Task<Hierarchy> GetHierarchyByIdAsync(int hierarchyId);
        Task<User> GetCeoByHierarchyIdAsync(int hierarchyId);

        Task DeleteSkillFromUser(Guid userId, string skill);
        Task AddSkillToUser(Guid userId, string skill);

        Task<List<string>> GetSkillsByUser(Guid UserId);


        Task AddAsync(User user);
        void Update(User user);
        void Delete(User user);
    }
}
