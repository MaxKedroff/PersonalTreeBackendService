using Application.Interfaces;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly UserDb _context;
        private readonly IUserRepository _userRepository;

        public UnitOfWork(
            UserDb context,
            IUserRepository userRepository)
        {
            _context = context;
            _userRepository = userRepository;
        }

        public IUserRepository UserRepository => _userRepository;

        public async Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public Task BeginTransactionAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void BeginTransaction() { }
        public Task CommitTransactionAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void CommitTransaction() { }
        public Task RollbackTransactionAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void RollbackTransaction() { }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
