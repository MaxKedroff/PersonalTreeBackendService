// Application.UnitTests/Services/UserMoveServiceTest.cs
using Application.Dtos;
using Application.Services;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.UnitTests.Services
{
    public class UserMoveServiceTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly UserService _userService;

        public UserMoveServiceTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockMemoryCache = new Mock<IMemoryCache>();

            var cacheEntry = Mock.Of<ICacheEntry>();
            _mockMemoryCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);

            _userService = new UserService(_mockUserRepository.Object, _mockLogger.Object, _mockMemoryCache.Object);
        }

        [Fact]
        public async Task MoveUserToHierarchyAsync_WithValidData_ShouldMoveUserSuccessfully()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Admin";
            var moveRequest = new MoveUserRequestDto
            {
                UserId = Guid.NewGuid(),
                TargetHierarchyId = 44,
                NewManagerId = Guid.NewGuid()
            };

            var userToMove = CreateTestUser(moveRequest.UserId, "User", "ToMove", "Developer", "Old Department", hierarchyId: 1);
            var targetHierarchy = new Hierarchy { HierarchyId = 44, LevelHierarchy = 4, TitleHierarchy = "Target Department", ColorHierarchy = "FF5733" };
            var newManager = CreateTestUser(moveRequest.NewManagerId.Value, "New", "Manager", "Team Lead", "Target Department", hierarchyId: 44);

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
                .ReturnsAsync(userToMove);
            _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(targetHierarchy);
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.NewManagerId.Value))
                .ReturnsAsync(newManager);
            _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            object cachedValue = null;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue)).Returns(false);

            var result = await _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole);

            Assert.NotNull(result);
            _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
                u.HierarchyId == moveRequest.TargetHierarchyId &&
                u.Manager_id == moveRequest.NewManagerId &&
                u.WorkInfo.Department == targetHierarchy.TitleHierarchy
            )), Times.Once);
        }

        //[Fact]
        //public async Task MoveUserToHierarchyAsync_WithoutManager_ShouldAutoAssignCeo()
        //{
        //    var currentUserId = Guid.NewGuid();
        //    var currentUserRole = "Hr";
        //    var moveRequest = new MoveUserRequestDto
        //    {
        //        UserId = Guid.NewGuid(),
        //        TargetHierarchyId = 44
        //    };

        //    var userToMove = CreateTestUser(moveRequest.UserId, "User", "ToMove", "Developer", "Old Department", hierarchyId: 1);
        //    var targetHierarchy = new Hierarchy { HierarchyId = 44, LevelHierarchy = 4, TitleHierarchy = "Target Department", ColorHierarchy = "FF5733" };
        //    var hierarchyCeo = CreateTestUser(Guid.NewGuid(), "Hierarchy", "CEO", "Manager", "Target Department", hierarchyId: 44);

        //    _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
        //        .ReturnsAsync(userToMove);
        //    _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
        //        .ReturnsAsync(targetHierarchy);
        //    _mockUserRepository.Setup(x => x.GetCeoByHierarchyIdAsync(moveRequest.TargetHierarchyId))
        //        .ReturnsAsync(hierarchyCeo);
        //    _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
        //        .Returns(Task.CompletedTask);

        //    var result = await _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole);

        //    Assert.NotNull(result);
        //    _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
        //        u.Manager_id == hierarchyCeo.User_id
        //    )), Times.Once);
        //}

        [Fact]
        public async Task MoveUserToHierarchyAsync_WithNonAdminRole_ShouldThrowUnauthorized()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "User";
            var moveRequest = new MoveUserRequestDto
            {
                UserId = Guid.NewGuid(),
                TargetHierarchyId = 44
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole));
        }

        [Fact]
        public async Task MoveUserToHierarchyAsync_WithCeoUser_ShouldThrowException()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Admin";
            var moveRequest = new MoveUserRequestDto
            {
                UserId = Guid.NewGuid(),
                TargetHierarchyId = 44
            };

            var userToMove = CreateTestUser(moveRequest.UserId, "CEO", "User", "Manager", "Department", hierarchyId: 1);
            userToMove.Subordinates = new List<User> { CreateTestUser(Guid.NewGuid(), "Sub", "Ordinate", "Dev", "Department") };

            var targetHierarchy = new Hierarchy { HierarchyId = 44, LevelHierarchy = 4, TitleHierarchy = "Target Department", ColorHierarchy = "FF5733" };

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
                .ReturnsAsync(userToMove);
            _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(targetHierarchy);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole));
        }

        [Fact]
        public async Task MoveUserToHierarchyAsync_WithNonLeafHierarchy_ShouldThrowException()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Admin";
            var moveRequest = new MoveUserRequestDto
            {
                UserId = Guid.NewGuid(),
                TargetHierarchyId = 1
            };

            var userToMove = CreateTestUser(moveRequest.UserId, "User", "ToMove", "Developer", "Department", hierarchyId: 1);
            var targetHierarchy = new Hierarchy { HierarchyId = 1, LevelHierarchy = 1, TitleHierarchy = "Root Department", ColorHierarchy = "FF5733" };

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
                .ReturnsAsync(userToMove);
            _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(targetHierarchy);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole));
        }

        [Fact]
        public async Task MoveUserToHierarchyAsync_WithManagerInDifferentHierarchy_ShouldThrowException()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Admin";
            var moveRequest = new MoveUserRequestDto
            {
                UserId = Guid.NewGuid(),
                TargetHierarchyId = 44,
                NewManagerId = Guid.NewGuid()
            };

            var userToMove = CreateTestUser(moveRequest.UserId, "User", "ToMove", "Developer", "Department", hierarchyId: 1);
            var targetHierarchy = new Hierarchy { HierarchyId = 44, LevelHierarchy = 4, TitleHierarchy = "Target Department", ColorHierarchy = "FF5733" };
            var newManager = CreateTestUser(moveRequest.NewManagerId.Value, "New", "Manager", "Team Lead", "Different Department", hierarchyId: 45);

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
                .ReturnsAsync(userToMove);
            _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(targetHierarchy);
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.NewManagerId.Value))
                .ReturnsAsync(newManager);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole));
        }

        

        [Fact]
        public async Task MoveUserToHierarchyAsync_WithValidBecomeCeo_ShouldPromoteUserToCeo()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Admin";
            var moveRequest = new MoveUserRequestDto
            {
                UserId = Guid.NewGuid(),
                TargetHierarchyId = 44,
                BecomeCeo = true
            };

            var currentCeo = CreateTestUser(Guid.NewGuid(), "Current", "CEO", "Manager", "Department A", hierarchyId: 44, managerId: null);
            var userToMove = CreateTestUser(moveRequest.UserId, "User", "ToMove", "Developer", "Department A", hierarchyId: 44, managerId: currentCeo.User_id);

            currentCeo.Subordinates = new List<User> { userToMove };

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
                .ReturnsAsync(userToMove);
            _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(new Hierarchy { HierarchyId = 44, LevelHierarchy = 4, TitleHierarchy = "Department A", ColorHierarchy = "FF5733" });
            _mockUserRepository.Setup(x => x.GetCeoByHierarchyIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(currentCeo);
            _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            var result = await _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole);

            Assert.NotNull(result);
            _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
                u.User_id == moveRequest.UserId && u.Manager_id == null
            )), Times.Once);
            _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
                u.User_id == currentCeo.User_id && u.Manager_id == moveRequest.UserId
            )), Times.Once);
        }

        [Fact]
        public async Task MoveUserToHierarchyAsync_WithBecomeCeoInDifferentDepartment_ShouldThrowException()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Admin";
            var moveRequest = new MoveUserRequestDto
            {
                UserId = Guid.NewGuid(),
                TargetHierarchyId = 44,
                BecomeCeo = true
            };

            var userToMove = CreateTestUser(moveRequest.UserId, "User", "ToMove", "Developer", "Department B", hierarchyId: 45, managerId: Guid.NewGuid());

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
                .ReturnsAsync(userToMove);
            _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(new Hierarchy { HierarchyId = 44, LevelHierarchy = 4, TitleHierarchy = "Department A", ColorHierarchy = "FF5733" });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole));
        }

        [Fact]
        public async Task MoveUserToHierarchyAsync_WithBecomeCeoButNotSubordinate_ShouldThrowException()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Admin";
            var moveRequest = new MoveUserRequestDto
            {
                UserId = Guid.NewGuid(),
                TargetHierarchyId = 44,
                BecomeCeo = true
            };

            var currentCeo = CreateTestUser(Guid.NewGuid(), "Current", "CEO", "Manager", "Department A", hierarchyId: 44, managerId: null);
            var userToMove = CreateTestUser(moveRequest.UserId, "User", "ToMove", "Developer", "Department A", hierarchyId: 44, managerId: Guid.NewGuid());

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
                .ReturnsAsync(userToMove);
            _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(new Hierarchy { HierarchyId = 44, LevelHierarchy = 4, TitleHierarchy = "Department A", ColorHierarchy = "FF5733" });
            _mockUserRepository.Setup(x => x.GetCeoByHierarchyIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(currentCeo);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole));
        }

        [Fact]
        public async Task MoveUserToHierarchyAsync_WithBecomeCeoNoCurrentCeo_ShouldThrowException()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Admin";
            var moveRequest = new MoveUserRequestDto
            {
                UserId = Guid.NewGuid(),
                TargetHierarchyId = 44,
                BecomeCeo = true
            };

            var userToMove = CreateTestUser(moveRequest.UserId, "User", "ToMove", "Developer", "Department A", hierarchyId: 44, managerId: Guid.NewGuid());

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
                .ReturnsAsync(userToMove);
            _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(new Hierarchy { HierarchyId = 44, LevelHierarchy = 4, TitleHierarchy = "Department A", ColorHierarchy = "FF5733" });
            _mockUserRepository.Setup(x => x.GetCeoByHierarchyIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync((User)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole));
        }

        //[Fact]
        //public async Task MoveUserToHierarchyAsync_WithManagerSubordinateSwap_ShouldRedistributeSubordinates()
        //{
        //    var currentUserId = Guid.NewGuid();
        //    var currentUserRole = "Admin";
        //    var subordinateId = Guid.NewGuid();
        //    var moveRequest = new MoveUserRequestDto
        //    {
        //        UserId = Guid.NewGuid(),
        //        TargetHierarchyId = 44,
        //        SwapWithUserId = subordinateId
        //    };

        //    var manager = CreateTestUser(moveRequest.UserId, "Manager", "User", "Team Lead", "Department A", hierarchyId: 1);
        //    var subordinate = CreateTestUser(subordinateId, "Subordinate", "User", "Developer", "Department A", hierarchyId: 1, managerId: manager.User_id);
        //    var otherSubordinate = CreateTestUser(Guid.NewGuid(), "Other", "Sub", "Developer", "Department A", hierarchyId: 1, managerId: manager.User_id);

        //    manager.Subordinates = new List<User> { subordinate, otherSubordinate };
        //    subordinate.Subordinates = new List<User>();

        //    _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
        //        .ReturnsAsync(manager);
        //    _mockUserRepository.Setup(x => x.GetUsersByIdAsync(subordinateId))
        //        .ReturnsAsync(subordinate);
        //    _mockUserRepository.Setup(x => x.GetUsersByIdAsync(otherSubordinate.User_id))
        //        .ReturnsAsync(otherSubordinate);
        //    _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
        //        .ReturnsAsync(new Hierarchy { HierarchyId = 44, LevelHierarchy = 4, TitleHierarchy = "Target Department", ColorHierarchy = "FF5733" });
        //    _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
        //        .Returns(Task.CompletedTask);

        //    var result = await _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole);

        //    Assert.NotNull(result);
        //    _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
        //        u.User_id == otherSubordinate.User_id && u.Manager_id == subordinateId
        //    )), Times.Once);
        //    _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
        //        u.User_id == moveRequest.UserId && u.Manager_id == subordinateId
        //    )), Times.Once);
        //}

        [Fact]
        public async Task MoveUserToHierarchyAsync_WithSubordinateManagerSwap_ShouldRedistributeSubordinates()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Admin";
            var managerId = Guid.NewGuid();
            var moveRequest = new MoveUserRequestDto
            {
                UserId = Guid.NewGuid(),
                TargetHierarchyId = 44,
                SwapWithUserId = managerId
            };

            var subordinate = CreateTestUser(moveRequest.UserId, "Subordinate", "User", "Developer", "Department A", hierarchyId: 1, managerId: managerId);
            var manager = CreateTestUser(managerId, "Manager", "User", "Team Lead", "Department A", hierarchyId: 1);
            var otherSubordinate = CreateTestUser(Guid.NewGuid(), "Other", "Sub", "Developer", "Department A", hierarchyId: 1, managerId: managerId);

            manager.Subordinates = new List<User> { subordinate, otherSubordinate };
            subordinate.Subordinates = new List<User>();

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
                .ReturnsAsync(subordinate);
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(managerId))
                .ReturnsAsync(manager);
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(otherSubordinate.User_id))
                .ReturnsAsync(otherSubordinate);
            _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(new Hierarchy { HierarchyId = 44, LevelHierarchy = 4, TitleHierarchy = "Target Department", ColorHierarchy = "FF5733" });
            _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            var result = await _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole);

            Assert.NotNull(result);
            _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
                u.User_id == otherSubordinate.User_id && u.Manager_id == moveRequest.UserId
            )), Times.Once);
            _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
                u.User_id == managerId && u.Manager_id == moveRequest.UserId
            )), Times.Once);
        }

        

        [Fact]
        public async Task MoveUserToHierarchyAsync_WithBecomeCeoAndSubordinates_ShouldReassignSubordinates()
        {
            var currentUserId = Guid.NewGuid();
            var currentUserRole = "Admin";
            var moveRequest = new MoveUserRequestDto
            {
                UserId = Guid.NewGuid(),
                TargetHierarchyId = 44,
                BecomeCeo = true
            };

            var currentCeo = CreateTestUser(Guid.NewGuid(), "Current", "CEO", "Manager", "Department A", hierarchyId: 44, managerId: null);
            var userToMove = CreateTestUser(moveRequest.UserId, "User", "ToMove", "Team Lead", "Department A", hierarchyId: 44, managerId: currentCeo.User_id);
            var subordinate1 = CreateTestUser(Guid.NewGuid(), "Sub", "Ordinate1", "Developer", "Department A", hierarchyId: 44, managerId: userToMove.User_id);
            var subordinate2 = CreateTestUser(Guid.NewGuid(), "Sub", "Ordinate2", "Developer", "Department A", hierarchyId: 44, managerId: userToMove.User_id);
            var otherSubordinate = CreateTestUser(Guid.NewGuid(), "Other", "Sub", "Developer", "Department A", hierarchyId: 44, managerId: currentCeo.User_id);

            userToMove.Subordinates = new List<User> { subordinate1, subordinate2 };
            currentCeo.Subordinates = new List<User> { userToMove, otherSubordinate };

            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(moveRequest.UserId))
                .ReturnsAsync(userToMove);
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(currentCeo.User_id))
                .ReturnsAsync(currentCeo);
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(subordinate1.User_id))
                .ReturnsAsync(subordinate1);
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(subordinate2.User_id))
                .ReturnsAsync(subordinate2);
            _mockUserRepository.Setup(x => x.GetUsersByIdAsync(otherSubordinate.User_id))
                .ReturnsAsync(otherSubordinate);

            _mockUserRepository.Setup(x => x.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(new Hierarchy
                {
                    HierarchyId = 44,
                    LevelHierarchy = 4,
                    TitleHierarchy = "Department A",
                    ColorHierarchy = "#FF5733"
                });
            _mockUserRepository.Setup(x => x.GetCeoByHierarchyIdAsync(moveRequest.TargetHierarchyId))
                .ReturnsAsync(currentCeo);
            _mockUserRepository.Setup(x => x.UpdateUserAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            var result = await _userService.MoveUserToHierarchyAsync(moveRequest, currentUserId, currentUserRole);

            Assert.NotNull(result);

            _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
                u.User_id == subordinate1.User_id && u.Manager_id == currentCeo.User_id
            )), Times.Once);
            _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
                u.User_id == subordinate2.User_id && u.Manager_id == currentCeo.User_id
            )), Times.Once);

            _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
                u.User_id == otherSubordinate.User_id && u.Manager_id == userToMove.User_id
            )), Times.Once);

            _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
                u.User_id == userToMove.User_id && u.Manager_id == null
            )), Times.Once);

            _mockUserRepository.Verify(x => x.UpdateUserAsync(It.Is<User>(u =>
                u.User_id == currentCeo.User_id && u.Manager_id == userToMove.User_id
            )), Times.Once);
        }

        private User CreateTestUser(Guid id, string lastName, string firstName, string position, string department, int? hierarchyId = null, Guid? managerId = null)
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
                    Avatar = null
                },
                Manager_id = managerId,
                HierarchyId = hierarchyId,
                Subordinates = new List<User>()
            };
        }
    }
}