using Application.Dtos;
using Application.Services;
using Castle.Core.Logging;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UnitTests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockMemoryCache = new Mock<IMemoryCache>();

            var cacheEntry = Mock.Of<ICacheEntry>();
            _mockMemoryCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);

            _userService = new UserService(_mockUserRepository.Object, _mockLogger.Object, _mockMemoryCache.Object);
        }

        [Fact]
        public async Task GetUserTableAsync_ValidRequest_ReturnsTableData()
        {
            var request = new TableRequestDto
            {
                page = 1,
                Limit = 10,
                Sort = "username_asc"
            };

            var users = new List<User>
            {
                CreateTestUser(Guid.NewGuid(), "John", "Doe", "Developer", "IT")
            };

            _mockUserRepository.Setup(x => x.GetUsersPagedAsync(1, 10, "username", "asc", null, null))
                .ReturnsAsync((users, 1));

            var result = await _userService.GetUserTableAsync(request);

            Assert.NotNull(result);
            Assert.Equal(1, result.AmountOfUsers);
            Assert.Single(result.UsersTable);
        }

        [Fact]
        public async Task GetUserTableAsync_InvalidPage_ThrowsException()
        {
            var request = new TableRequestDto { page = 0, Limit = 10 };
            await Assert.ThrowsAsync<ArgumentException>(() => _userService.GetUserTableAsync(request));
        }

        [Fact]
        public async Task GetUserTableAsync_InvalidLimit_ThrowsException()
        {
            var request = new TableRequestDto { page = 1, Limit = 0 };
            await Assert.ThrowsAsync<ArgumentException>(() => _userService.GetUserTableAsync(request));
        }

        [Fact]
        public async Task GetUserDetailAsync_ValidId_ReturnsUserDetails()
        {
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId, "John", "Doe", "Developer", "IT");

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync(user);
            var result = await _userService.GetUserDetailAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(userId, result.User_id);
        }

        [Fact]
        public async Task GetUserDetailAsync_InvalidId_ThrowsException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _userService.GetUserDetailAsync(Guid.Empty));
        }

        [Fact]
        public async Task GetUserDetailAsync_UserNotFound_ThrowsException()
        {
            var userId = Guid.NewGuid();
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync((User)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.GetUserDetailAsync(userId));
        }

        [Fact]
        public async Task GetSearchResultAsync_ValidRequest_ReturnsResults()
        {
            var request = new SearchRequestDto
            {
                searchCriteria = "name",
                searchValue = "Doe", 
                queryAmount = 10
            };

            var users = new List<User>
    {
        CreateTestUser(Guid.NewGuid(), "Doe", "John", "Developer", "IT")
    };

            _mockUserRepository.Setup(x => x.GetSearchResultAsync("name", "Doe", 10))
                .ReturnsAsync(users);

            var result = await _userService.GetSearchResultAsync(request);

            Assert.NotNull(result);
            Assert.Equal(1, result.amount);
            Assert.Single(result.searchItems);
            Assert.Equal("Doe John", result.searchItems.First().username);
        }

        [Fact]
        public async Task GetSearchResultAsync_EmptySearchValue_ReturnsEmptyResults()
        {
            var request = new SearchRequestDto { searchValue = "" };
            var result = await _userService.GetSearchResultAsync(request);

            Assert.NotNull(result);
            Assert.Equal(0, result.amount);
            Assert.Empty(result.searchItems);
        }

        [Fact]
        public async Task GetDepartmentHierarchyAsync_ValidData_ReturnsHierarchy()
        {
            var ceo = CreateTestUser(Guid.NewGuid(), "User", "CEO", "CEO", "Management"); 
            var employees = new List<User>
    {
        ceo,
        CreateTestUser(Guid.NewGuid(), "Doe", "John", "Developer", "IT", ceo.User_id)
    };

            _mockUserRepository.Setup(x => x.GetCeoAsync()).ReturnsAsync(ceo);
            _mockUserRepository.Setup(x => x.GetUsersWithHierarchyAsync()).ReturnsAsync(employees);

            object cachedValue = null;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue)).Returns(false);

            var result = await _userService.GetDepartmentHierarchyAsync();

            Assert.NotNull(result);
            Assert.NotNull(result.Ceo);
            Assert.Equal("User CEO", result.Ceo.UserName);
            Assert.Equal(2, result.TotalEmployees);
        }

        [Fact]
        public async Task GetDepartmentHierarchyAsync_NoCeo_ReturnsHierarchyWithoutCeo()
        {
            var employees = new List<User>
            {
                CreateTestUser(Guid.NewGuid(), "John", "Doe", "Developer", "IT")
            };

            _mockUserRepository.Setup(x => x.GetCeoAsync()).ReturnsAsync((User)null);
            _mockUserRepository.Setup(x => x.GetUsersWithHierarchyAsync()).ReturnsAsync(employees);

            object cachedValue = null;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue)).Returns(false);
            var result = await _userService.GetDepartmentHierarchyAsync();
            Assert.NotNull(result);
            Assert.Null(result.Ceo);
            Assert.Equal(1, result.TotalEmployees);
        }

        [Theory]
        [InlineData("username_asc", "username", "asc")]
        [InlineData("position_desc", "position", "desc")]
        [InlineData("department_asc", "department", "asc")]
        [InlineData("", null, "asc")]
        [InlineData("invalid", null, "asc")]
        public void ParseSortParameter_VariousInputs_ParsersCorrectly(string input, string expectedField, string expectedOrder)
        {
            var result = _userService.TestParseSortParameter(input);

            Assert.Equal(expectedField, result.Field);
            Assert.Equal(expectedOrder, result.Order);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_UserUpdatesOwnProfile_ReturnsUpdatedUser()
        {
            var userId = Guid.NewGuid();
            var currentUserId = userId;
            var currentUserRole = "User";
            var updateDto = new UpdateProfileDto
            {
                Phone = "+7-999-123-45-67",
                City = "Москва",
                Interests = "программирование, фотография",
                Avatar = "data:image/png;base64,test",
                Contacts = new Dictionary<string, object>
                {
                    { "telegram", "@testuser" },
                    { "github", "testuser" }
                }
            };

            var existingUser = CreateTestUser(userId, "Иванов", "Иван", "Developer", "IT");
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync(existingUser);
            _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            var result = await _userService.UpdateUserProfileAsync(userId, currentUserId, currentUserRole, updateDto);

            Assert.NotNull(result);
            Assert.Equal(updateDto.Phone, existingUser.ContactInfo.Phone);
            Assert.Equal(updateDto.City, existingUser.ContactInfo.City);
            Assert.Equal(updateDto.Interests, existingUser.PersonalInfo.Interests);
            Assert.Equal(updateDto.Avatar, existingUser.ContactInfo.Avatar);

            _mockUserRepository.Verify(x => x.UpdateUserAsync(existingUser), Times.Once);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_AdminUpdatesOtherUser_ReturnsUpdatedUser()
        {
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Admin";
            var updateDto = new UpdateProfileDto
            {
                Phone = "+7-495-111-11-11",
                City = "Санкт-Петербург",
                Interests = "управление, стратегия"
            };

            var existingUser = CreateTestUser(userId, "Петров", "Петр", "Manager", "Management");
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync(existingUser);
            _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            var result = await _userService.UpdateUserProfileAsync(userId, currentUserId, currentUserRole, updateDto);

            Assert.NotNull(result);
            Assert.Equal(updateDto.Phone, existingUser.ContactInfo.Phone);
            Assert.Equal(updateDto.City, existingUser.ContactInfo.City);
            Assert.Equal(updateDto.Interests, existingUser.PersonalInfo.Interests);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_HrUpdatesOtherUser_ReturnsUpdatedUser()
        {
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Hr";
            var updateDto = new UpdateProfileDto
            {
                Phone = "+7-495-222-22-22",
                City = "Казань",
                Interests = "рекрутинг, психология"
            };

            var existingUser = CreateTestUser(userId, "Сидорова", "Анна", "HR Specialist", "HR");
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync(existingUser);
            _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            var result = await _userService.UpdateUserProfileAsync(userId, currentUserId, currentUserRole, updateDto);

            Assert.NotNull(result);
            Assert.Equal(updateDto.Phone, existingUser.ContactInfo.Phone);
            Assert.Equal(updateDto.City, existingUser.ContactInfo.City);
            Assert.Equal(updateDto.Interests, existingUser.PersonalInfo.Interests);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_UserTriesToUpdateOtherUser_ThrowsUnauthorizedAccessException()
        {
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid(); 
            var currentUserRole = "User";
            var updateDto = new UpdateProfileDto { Phone = "+7-999-999-99-99" };

            var existingUser = CreateTestUser(userId, "Иванов", "Иван", "Developer", "IT");
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync(existingUser);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _userService.UpdateUserProfileAsync(userId, currentUserId, currentUserRole, updateDto));
        }

        [Fact]
        public async Task UpdateUserProfileAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            var userId = Guid.NewGuid();
            var currentUserId = userId;
            var currentUserRole = "User";
            var updateDto = new UpdateProfileDto { Phone = "+7-999-999-99-99" };

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync((User)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _userService.UpdateUserProfileAsync(userId, currentUserId, currentUserRole, updateDto));
        }

        [Fact]
        public async Task UpdateUserProfileAsync_InvalidAvatarFormat_ThrowsArgumentException()
        {
            var userId = Guid.NewGuid();
            var currentUserId = userId;
            var currentUserRole = "User";
            var updateDto = new UpdateProfileDto
            {
                Avatar = "invalid_avatar_format"
            };

            var existingUser = CreateTestUser(userId, "Иванов", "Иван", "Developer", "IT");
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync(existingUser);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _userService.UpdateUserProfileAsync(userId, currentUserId, currentUserRole, updateDto));
        }

        [Fact]
        public async Task UpdateUserProfileAsync_ValidHttpAvatar_UpdatesSuccessfully()
        {
            var userId = Guid.NewGuid();
            var currentUserId = userId;
            var currentUserRole = "User";
            var updateDto = new UpdateProfileDto
            {
                Avatar = "https://example.com/avatar.jpg"
            };

            var existingUser = CreateTestUser(userId, "Иванов", "Иван", "Developer", "IT");
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync(existingUser);
            _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            var result = await _userService.UpdateUserProfileAsync(userId, currentUserId, currentUserRole, updateDto);

            Assert.NotNull(result);
            Assert.Equal(updateDto.Avatar, existingUser.ContactInfo.Avatar);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_ValidBase64Avatar_UpdatesSuccessfully()
        {
            var userId = Guid.NewGuid();
            var currentUserId = userId;
            var currentUserRole = "User";
            var updateDto = new UpdateProfileDto
            {
                Avatar = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/"
            };

            var existingUser = CreateTestUser(userId, "Иванов", "Иван", "Developer", "IT");
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync(existingUser);
            _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            var result = await _userService.UpdateUserProfileAsync(userId, currentUserId, currentUserRole, updateDto);

            Assert.NotNull(result);
            Assert.Equal(updateDto.Avatar, existingUser.ContactInfo.Avatar);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_PartialUpdate_UpdatesOnlyProvidedFields()
        {
            var userId = Guid.NewGuid();
            var currentUserId = userId;
            var currentUserRole = "User";
            var originalPhone = "+7-495-000-00-00";
            var originalCity = "Москва";

            var existingUser = CreateTestUser(userId, "Иванов", "Иван", "Developer", "IT");
            existingUser.ContactInfo.Phone = originalPhone;
            existingUser.ContactInfo.City = originalCity;

            var updateDto = new UpdateProfileDto
            {
                Phone = "+7-999-123-45-67"
            };

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync(existingUser);
            _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _userService.UpdateUserProfileAsync(userId, currentUserId, currentUserRole, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.Phone, existingUser.ContactInfo.Phone); 
            Assert.Equal(originalCity, existingUser.ContactInfo.City); 
            Assert.Equal(originalPhone, "+7-495-000-00-00"); 
        }

        [Fact]
        public async Task UpdateUserProfileAsync_EmptyContacts_UpdatesSuccessfully()
        {
            var userId = Guid.NewGuid();
            var currentUserId = userId;
            var currentUserRole = "User";
            var updateDto = new UpdateProfileDto
            {
                Phone = "+7-999-123-45-67",
                Contacts = new Dictionary<string, object>() 
            };

            var existingUser = CreateTestUser(userId, "Иванов", "Иван", "Developer", "IT");
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(userId))
                .ReturnsAsync(existingUser);
            _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            var result = await _userService.UpdateUserProfileAsync(userId, currentUserId, currentUserRole, updateDto);

            Assert.NotNull(result);
            Assert.Equal(updateDto.Phone, existingUser.ContactInfo.Phone);
        }



        private User CreateTestUser(Guid id, string lastName, string firstName, string position, string department, Guid? managerId = null)
        {
            return new User
            {
                User_id = id,
                Login = "testlogin",
                Password = "testpassword",
                Email = "test@test.com",
                SamAccountName = "testuser",
                AdGuid = Guid.NewGuid().ToString(),
                IsActive = true,
                Created_at = DateTime.UtcNow,
                Updated_at = DateTime.UtcNow,
                PersonalInfo = new PersonalInfo
                {
                    First_name = firstName,
                    Last_name = lastName,
                    Patronymic = null,
                    Birth_date = new DateTime(1990, 1, 1)
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
                    Avatar = null,
                    New_avatar = null
                },
                Manager_id = managerId
            };
        }
    }
}
