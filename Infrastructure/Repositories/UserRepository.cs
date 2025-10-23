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

        public async Task<List<User>> GetUsersAsync()
        {
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
    }
}
