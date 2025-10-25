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
    }
}
