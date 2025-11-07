using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UnitTests.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly UserDb _context;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<UserDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new UserDb(options);
            _repository = new UserRepository(_context);

            SeedTestData();
        }

        [Fact]
        public async Task GetUsersByIdAsync_ValidId_ReturnsUser()
        {
            var userId = Guid.NewGuid();
            var expectedUser = CreateTestUser(userId, "Doe", "John");

            _context.Users.RemoveRange(_context.Users);
            await _context.SaveChangesAsync();

            _context.Users.Add(expectedUser);
            await _context.SaveChangesAsync();
            var result = await _repository.GetUsersByIdAsync(userId);
            Assert.NotNull(result);
            Assert.Equal(userId, result.User_id);
            Assert.Equal("Doe", result.PersonalInfo.Last_name);
            Assert.Equal("John", result.PersonalInfo.First_name);
        }

        [Fact]
        public async Task GetUsersByIdAsync_InvalidId_ReturnsNull()
        {
            var result = await _repository.GetUsersByIdAsync(Guid.NewGuid());
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCeoAsync_WithCeo_ReturnsCeo()
        {
            var ceo = CreateTestUser(Guid.NewGuid(), "CEO", "User");
            ceo.Manager_id = null;
            var result = await _repository.GetCeoAsync();
            Assert.NotNull(result);
            Assert.Null(result.Manager_id);
        }

        //[Fact]
        //public async Task GetSearchResultAsync_ByName_ReturnsMatchingUsers()
        //{
        //    var result = await _repository.GetSearchResultAsync("name", "Doe", 10);
        //    Assert.NotNull(result);
        //    Assert.All(result, user =>
        //        Assert.Contains("Doe", user.PersonalInfo.Last_name, StringComparison.OrdinalIgnoreCase));
        //}

        //[Fact]
        //public async Task GetSearchResultAsync_EmptySearch_ReturnsEmptyList()
        //{
        //    var result = await _repository.GetSearchResultAsync("name", "", 10);
        //    Assert.NotNull(result);
        //    Assert.Empty(result);
        //}

        [Fact]
        public async Task GetUsersPagedAsync_ValidParameters_ReturnsPagedResults()
        {
            var result = await _repository.GetUsersPagedAsync(1, 5, "username", "asc");
            Assert.NotNull(result.Users);
            Assert.True(result.TotalCount > 0);
            Assert.True(result.Users.Count <= 5);
        }

        [Fact]
        public async Task GetUsersPagedAsync_WithFilters_ReturnsFilteredResults()
        {
            var result = await _repository.GetUsersPagedAsync(1, 10, null, "asc", "Developer", "IT");
            Assert.NotNull(result.Users);
            Assert.All(result.Users, user =>
            {
                Assert.Equal("Developer", user.WorkInfo.Position);
                Assert.Equal("IT", user.WorkInfo.Department);
            });
        }

        [Fact]
        public async Task GetUsersWithHierarchyAsync_ReturnsUsersWithRelations()
        {
            var result = await _repository.GetUsersWithHierarchyAsync();
            Assert.NotNull(result);
            Assert.All(result, user => Assert.True(user.IsActive));
        }

        private void SeedTestData()
        {
            var users = new List<User>
            {
                CreateTestUser(Guid.NewGuid(), "Doe", "John", "Developer", "IT"),
                CreateTestUser(Guid.NewGuid(), "Smith", "Jane", "Manager", "HR"),
                CreateTestUser(Guid.NewGuid(), "Johnson", "Bob", "Developer", "IT"),
                CreateTestUser(Guid.NewGuid(), "User", "CEO", "CEO", "Management", null) // CEO без manager_id
            };

            _context.Users.AddRange(users);
            _context.SaveChanges();
        }

        private User CreateTestUser(Guid id, string lastName, string firstName,
    string position = "Developer", string department = "IT", Guid? managerId = null)
        {
            return new User
            {
                User_id = id,
                Login = $"{firstName.ToLower()}.{lastName.ToLower()}",
                Password = "testpassword",
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@company.com",
                SamAccountName = $"{firstName.ToLower()}.{lastName.ToLower()}",
                AdGuid = Guid.NewGuid().ToString(),
                IsActive = true,
                Created_at = DateTime.UtcNow,
                Updated_at = DateTime.UtcNow,
                PersonalInfo = new PersonalInfo
                {
                    First_name = firstName,
                    Last_name = lastName,
                    Patronymic = "",
                    Birth_date = new DateTime(1990, 1, 1),
                    Interests = "Test interests",
                    
                },
                WorkInfo = new WorkInfo
                {
                    Position = position,
                    Department = department,
                    Work_exp = new DateTime(2020, 1, 1)
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1234567890",
                    City = "Test City",
                    Avatar = "",
                    New_avatar = ""
                },
                Manager_id = managerId
            };
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
