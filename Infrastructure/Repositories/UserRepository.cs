using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {

        private UserDb _context;

        public UserRepository(UserDb context)
        {
            _context = context;
        }

        public async Task<List<User>> GetSearchResultAsync(string criteria, string searchString, int queryAmount)
        {
            if (string.IsNullOrWhiteSpace(searchString))
                return new List<User>();



            var query = _context.Users
                .Include(u => u.PersonalInfo)
                .Include(u => u.WorkInfo)
                .AsQueryable();

            query = criteria?.ToLower() switch
            {
                "department" => query.Where(u => u.WorkInfo.Department.Contains(searchString)),
                "position" => query.Where(u => u.WorkInfo.Position.Contains(searchString)),
                "name" => query.Where(u => 
                    u.PersonalInfo.Last_name.Contains(searchString) ||
                    u.PersonalInfo.First_name.Contains(searchString) ||
                    u.PersonalInfo.Patronymic.Contains(searchString)),
                    _ => query.Where(u =>
                    u.PersonalInfo.Last_name.Contains(searchString) ||
                    u.PersonalInfo.First_name.Contains(searchString) ||
                    u.PersonalInfo.Patronymic.Contains(searchString) ||
                    u.WorkInfo.Department.Contains(searchString) ||
                    u.WorkInfo.Position.Contains(searchString))
            };

            return await query
                .Take(queryAmount)
                .ToListAsync();
        }

        public async Task<List<User>> GetUsersAsync()
        {
            // Сначала проверим без Include
            var simpleUsers = await _context.Users.ToListAsync();
            Console.WriteLine($"Simple query found {simpleUsers.Count} users");

            // Затем с Include
            return await _context.Users
                .Include(u => u.Manager)
                .Include(u => u.Subordinates)
                .Include(u => u.PersonalInfo)
                .Include(u => u.WorkInfo)
                .Include(u => u.ContactInfo)
                .ToListAsync();
        }




        public async Task<User> GetUsersByIdAsync(Guid UserId)
        {
            return await _context.Users
            .Include(u => u.Manager)
            .Include(u => u.Subordinates)
            .Include(u => u.PersonalInfo)
            .Include(u => u.WorkInfo)
            .Include(u => u.ContactInfo)
            .FirstOrDefaultAsync(u => u.User_id == UserId);
        }

        public async Task<List<User>> GetUsersWithHierarchyAsync()
        {
            return await _context.Users
                .Include(u => u.PersonalInfo)
                .Include(u => u.WorkInfo)
                .Include(u => u.ContactInfo)
                .Include(u => u.Subordinates)
                    .ThenInclude(sub => sub.PersonalInfo)
                .Include(u => u.Subordinates)
                    .ThenInclude(sub => sub.WorkInfo)
                .Include(u => u.Subordinates)
                    .ThenInclude(sub => sub.ContactInfo)
                .Where(u => u.IsActive)
                .ToListAsync();
        }

        public async Task<User> GetCeoAsync()
        {
            return await _context.Users
                .Include(u => u.PersonalInfo)
                .Include(u => u.WorkInfo)
                .Include(u => u.ContactInfo)
                .Include(u => u.Subordinates)
                    .ThenInclude(sub => sub.PersonalInfo)
                .Include(u => u.Subordinates)
                    .ThenInclude(sub => sub.WorkInfo)
                .Include(u => u.Subordinates)
                    .ThenInclude(sub => sub.ContactInfo)
                .FirstOrDefaultAsync(u => u.Manager_id == null && u.IsActive);
        }

        public async Task<(List<User> Users, int TotalCount)> GetUsersPagedAsync(
            int page, int pageSize, string sortBy = null, string sortOrder = "asc",
            string positionFilter = null, string departmentFilter = null, string searchText = null)
        {
            var users = await GetUsersAsync();

            Console.WriteLine($"=== DIAGNOSTICS START ===");
            Console.WriteLine($"Total users from GetUsersAsync(): {users.Count}");
            Console.WriteLine($"Users with WorkInfo: {users.Count(u => u.WorkInfo != null)}");
            Console.WriteLine($"Users without WorkInfo: {users.Count(u => u.WorkInfo == null)}");
            Console.WriteLine($"Search text: '{searchText}'");

            if (users.Any())
            {
                var sampleUser = users.First();
                Console.WriteLine($"Sample user - WorkInfo: {sampleUser.WorkInfo != null}, " +
                                 $"Position: {sampleUser.WorkInfo?.Position}, " +
                                 $"Department: {sampleUser.WorkInfo?.Department}");
            }

            // Фильтрация по отдельным полям
            var filteredUsers = users.AsQueryable();

            Console.WriteLine($"Before filtering: {filteredUsers.Count()} users");

            // Текстовый поиск по всем полям
            if (!string.IsNullOrEmpty(searchText))
            {
                Console.WriteLine($"Applying search text: '{searchText}'");
                filteredUsers = filteredUsers.Where(u =>
                    (u.WorkInfo != null && u.WorkInfo.Position != null &&
                     u.WorkInfo.Position.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (u.WorkInfo != null && u.WorkInfo.Department != null &&
                     u.WorkInfo.Department.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (u.PersonalInfo != null && (
                        (!string.IsNullOrEmpty(u.PersonalInfo.Last_name) &&
                         u.PersonalInfo.Last_name.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(u.PersonalInfo.First_name) &&
                         u.PersonalInfo.First_name.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(u.PersonalInfo.Patronymic) &&
                         u.PersonalInfo.Patronymic.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    ))
                );
                Console.WriteLine($"After search text filter: {filteredUsers.Count()} users");
            }

            if (!string.IsNullOrEmpty(positionFilter))
            {
                Console.WriteLine($"Applying position filter: '{positionFilter}'");
                filteredUsers = filteredUsers.Where(u =>
                    u.WorkInfo != null &&
                    u.WorkInfo.Position != null &&
                    u.WorkInfo.Position.Contains(positionFilter, StringComparison.OrdinalIgnoreCase));
                Console.WriteLine($"After position filter: {filteredUsers.Count()} users");
            }

            if (!string.IsNullOrEmpty(departmentFilter))
            {
                Console.WriteLine($"Applying department filter: '{departmentFilter}'");
                filteredUsers = filteredUsers.Where(u =>
                    u.WorkInfo != null &&
                    u.WorkInfo.Department != null &&
                    u.WorkInfo.Department.Contains(departmentFilter, StringComparison.OrdinalIgnoreCase));
                Console.WriteLine($"After department filter: {filteredUsers.Count()} users");
            }

            Console.WriteLine($"After all filters: {filteredUsers.Count()} users");

            // Сортировка
            if (!string.IsNullOrEmpty(sortBy))
            {
                Console.WriteLine($"Applying sort: {sortBy} {sortOrder}");
                filteredUsers = sortBy.ToLower() switch
                {
                    "username" => sortOrder?.ToLower() == "desc"
                        ? filteredUsers.OrderByDescending(u => u.GetFullName())
                        : filteredUsers.OrderBy(u => u.GetFullName()),
                    "position" => sortOrder?.ToLower() == "desc"
                        ? filteredUsers.OrderByDescending(u => u.WorkInfo != null ? u.WorkInfo.Position ?? "" : "")
                        : filteredUsers.OrderBy(u => u.WorkInfo != null ? u.WorkInfo.Position ?? "" : ""),
                    "department" => sortOrder?.ToLower() == "desc"
                        ? filteredUsers.OrderByDescending(u => u.WorkInfo != null ? u.WorkInfo.Department ?? "" : "")
                        : filteredUsers.OrderBy(u => u.WorkInfo != null ? u.WorkInfo.Department ?? "" : ""),
                    _ => filteredUsers.OrderBy(u => u.GetFullName())
                };
            }
            else
            {
                filteredUsers = filteredUsers.OrderBy(u => u.GetFullName());
            }

            var totalCount = filteredUsers.Count();
            Console.WriteLine($"Total count after sorting: {totalCount}");

            // Пагинация
            var pagedUsers = filteredUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            Console.WriteLine($"After pagination (page {page}, size {pageSize}): {pagedUsers.Count} users");
            Console.WriteLine($"=== DIAGNOSTICS END ===");

            return (pagedUsers, totalCount);
        }

        public async Task<User> GetUserByLoginAsync(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new ArgumentException("Login cannot be empty", nameof(login));
            }

            return await _context.Users
                .Include(u => u.PersonalInfo)
                .Include(u => u.WorkInfo)
                .Include(u => u.ContactInfo)
                .FirstOrDefaultAsync(u => u.Login.ToLower() == login.ToLower() && u.IsActive);
        }

        public async Task UpdateUserAsync(User user)
        {
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
