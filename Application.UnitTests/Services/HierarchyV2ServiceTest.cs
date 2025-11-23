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
using System.Text;
using System.Threading.Tasks;

namespace Application.UnitTests.Services
{
    public class HierarchyV2ServiceTest
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly UserService _userService;

        public HierarchyV2ServiceTest()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockMemoryCache = new Mock<IMemoryCache>();

            var cacheEntry = Mock.Of<ICacheEntry>();
            _mockMemoryCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);

            _userService = new UserService(_mockUserRepository.Object, _mockLogger.Object, _mockMemoryCache.Object);
        }

        [Fact]
        public async Task GetDepartmentHierarchyAsync_WithValidHierarchyAndUsers_ReturnsCorrectTree()
        {
            var hierarchies = CreateHierarchyList();
            var users = CreateUsersWithHierarchy(hierarchies);

            _mockUserRepository.Setup(x => x.GetHierarchiesList()).ReturnsAsync(hierarchies);
            _mockUserRepository.Setup(x => x.GetUsersWithHierarchyV2Async()).ReturnsAsync(users);

            object cachedValue = null;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue)).Returns(false);

            var result = await _userService.GetDepartmentHierarchyAsyncV2();

            Assert.NotNull(result);
            Assert.Equal(1, result.Level);
            Assert.Equal("UDV GROUP", result.Title);
            Assert.Equal(3, result.Children.Count);

            var digitalTransformation = result.Children.FirstOrDefault(c => c.Title == "UDV Digital Transformation");
            Assert.NotNull(digitalTransformation);
            Assert.Equal(2, digitalTransformation.Level);
            Assert.Equal(3, digitalTransformation.Children.Count); 

            var ftSoft = digitalTransformation.Children.FirstOrDefault(c => c.Title == "ФТ-СОФТ");
            Assert.NotNull(ftSoft);
            Assert.Equal(3, ftSoft.Level);
            Assert.Equal(5, ftSoft.Children.Count); 

            var osnovnoe = ftSoft.Children.FirstOrDefault(c => c.Title == "Основное подразделение");
            Assert.NotNull(osnovnoe);
            Assert.Equal(4, osnovnoe.Level);
            Assert.Single(osnovnoe.Children); 

            var analytics = osnovnoe.Children.First();
            Assert.Equal("Направление Аналитики и документации", analytics.Title);
            Assert.Equal(5, analytics.Level);
            Assert.Empty(analytics.Children); 

            Assert.NotNull(analytics.Manager);
            Assert.Equal("CEO ФТ-СОФТ", analytics.Manager.UserName);
            Assert.Equal("Руководитель", analytics.Manager.Position);
            Assert.Equal(2, analytics.Employees.Count);
            Assert.Contains(analytics.Employees, e => e.UserName == "Разработчик 1");
            Assert.Contains(analytics.Employees, e => e.UserName == "Разработчик 2");
        }


        [Fact]
        public async Task GetDepartmentHierarchyAsync_WithNoUsersInLeaf_ReturnsNodeWithoutEmployees()
        {
            var hierarchies = CreateHierarchyList();
            var users = CreateUsersWithHierarchy(hierarchies).Where(u => u.HierarchyId != 44).ToList();
            _mockUserRepository.Setup(x => x.GetHierarchiesList()).ReturnsAsync(hierarchies);
            _mockUserRepository.Setup(x => x.GetUsersWithHierarchyV2Async()).ReturnsAsync(users);

            object cachedValue = null;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue)).Returns(false);

            var result = await _userService.GetDepartmentHierarchyAsyncV2();
            var analyticsNode = FindNodeByTitle(result, "Направление Аналитики и документации");
            Assert.NotNull(analyticsNode);
            Assert.Null(analyticsNode.Manager);
            Assert.Empty(analyticsNode.Employees);
        }

        [Fact]
        public async Task GetDepartmentHierarchyAsync_WithNoHierarchyInDb_ReturnsSingleNodeWithNoChildren()
        {
            _mockUserRepository.Setup(x => x.GetHierarchiesList()).ReturnsAsync(new List<Hierarchy>());
            _mockUserRepository.Setup(x => x.GetUsersWithHierarchyV2Async()).ReturnsAsync(new List<User>());

            var hierarchies = new List<Hierarchy>();

            object cachedValue = null;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue)).Returns(false);

            var result = await _userService.GetDepartmentHierarchyAsyncV2();

            Assert.NotNull(result);
            Assert.Equal(1, result.Level);
            Assert.Equal("UDV GROUP", result.Title);
            Assert.Empty(result.Children);
            Assert.Null(result.Manager);
            Assert.Empty(result.Employees);
        }

        [Fact]
        public async Task GetDepartmentHierarchyAsync_CeoIsInLeaf_ReturnsManagerCorrectly()
        {
            var hierarchies = CreateHierarchyList();
            var ceo = CreateTestUser(Guid.NewGuid(), "CEO", "Main", "Руководитель", "Management", hierarchyId: 44);
            var dev = CreateTestUser(Guid.NewGuid(), "Dev", "Junior", "Разработчик", "IT", hierarchyId: 44, managerId: ceo.User_id);

            var users = new List<User> { ceo, dev };
            _mockUserRepository.Setup(x => x.GetHierarchiesList()).ReturnsAsync(hierarchies);

            _mockUserRepository.Setup(x => x.GetUsersWithHierarchyV2Async()).ReturnsAsync(users);

            object cachedValue = null;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue)).Returns(false);

            var result = await _userService.GetDepartmentHierarchyAsyncV2();

            var leaf = FindNodeByTitle(result, "Направление Аналитики и документации");
            Assert.NotNull(leaf);
            Assert.Equal("CEO Main", leaf.Manager.UserName);
            Assert.Single(leaf.Employees);
            Assert.Equal("Dev Junior", leaf.Employees.First().UserName);
        }

        [Fact]
        public async Task GetDepartmentHierarchyAsync_NoCeoInLeaf_FirstUserBecomesManager()
        {
            var hierarchies = CreateHierarchyList();
            var user1 = CreateTestUser(Guid.NewGuid(), "First", "User", "Аналитик", "IT", hierarchyId: 44);
            var user2 = CreateTestUser(Guid.NewGuid(), "Second", "User", "Тестировщик", "IT", hierarchyId: 44, managerId: user1.User_id);

            var users = new List<User> { user1, user2 };
            _mockUserRepository.Setup(x => x.GetHierarchiesList()).ReturnsAsync(hierarchies);

            _mockUserRepository.Setup(x => x.GetUsersWithHierarchyV2Async()).ReturnsAsync(users);

            object cachedValue = null;
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue)).Returns(false);

            var result = await _userService.GetDepartmentHierarchyAsyncV2();

            var leaf = FindNodeByTitle(result, "Направление Аналитики и документации");
            Assert.NotNull(leaf);
            Assert.Equal("First User", leaf.Manager.UserName);
            Assert.Single(leaf.Employees);
            Assert.Equal("Second User", leaf.Employees.First().UserName);
        }


        private List<Hierarchy> CreateHierarchyList()
        {
            return new List<Hierarchy>
            {
                new Hierarchy { HierarchyId = 1, ParentId = null, LevelHierarchy = 1, TitleHierarchy = "UDV GROUP", ColorHierarchy = "#000000" },

                new Hierarchy { HierarchyId = 2, ParentId = 1, LevelHierarchy = 2, TitleHierarchy = "UDV Digital Transformation", ColorHierarchy = "#FF5733" },
                new Hierarchy { HierarchyId = 3, ParentId = 1, LevelHierarchy = 2, TitleHierarchy = "UDV Security", ColorHierarchy = "#3498DB" },
                new Hierarchy { HierarchyId = 4, ParentId = 1, LevelHierarchy = 2, TitleHierarchy = "UDV Services", ColorHierarchy = "#2ECC71" },

                new Hierarchy { HierarchyId = 5, ParentId = 2, LevelHierarchy = 3, TitleHierarchy = "ТриниДата", ColorHierarchy = "#F39C12" },
                new Hierarchy { HierarchyId = 6, ParentId = 2, LevelHierarchy = 3, TitleHierarchy = "ВНЕ ОЧЕРЕДИ", ColorHierarchy = "#F39C12" },
                new Hierarchy { HierarchyId = 7, ParentId = 2, LevelHierarchy = 3, TitleHierarchy = "ФТ-СОФТ", ColorHierarchy = "#F39C12" },

                new Hierarchy { HierarchyId = 8, ParentId = 3, LevelHierarchy = 3, TitleHierarchy = "КИТ", ColorHierarchy = "#8E44AD" },
                new Hierarchy { HierarchyId = 9, ParentId = 3, LevelHierarchy = 3, TitleHierarchy = "КИТ.Р", ColorHierarchy = "#8E44AD" },
                new Hierarchy { HierarchyId = 10, ParentId = 3, LevelHierarchy = 3, TitleHierarchy = "Сайберлимфа", ColorHierarchy = "#8E44AD" },

                new Hierarchy { HierarchyId = 11, ParentId = 4, LevelHierarchy = 3, TitleHierarchy = "Витропс", ColorHierarchy = "#16A085" },
                // ТриниДата
                new Hierarchy { HierarchyId = 12, ParentId = 5, LevelHierarchy = 4, TitleHierarchy = "Основное подразделение", ColorHierarchy = "#7F8C8D" },
                // ВНЕ ОЧЕРЕДИ
                new Hierarchy { HierarchyId = 13, ParentId = 6, LevelHierarchy = 4, TitleHierarchy = "Основное подразделение", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 14, ParentId = 6, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктовой разработки", ColorHierarchy = "#7F8C8D" },
                // ФТ-СОФТ
                new Hierarchy { HierarchyId = 15, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Администрация", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 16, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктовой разработки 1", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 17, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктовой разработки 2", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 18, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Отдел заказной разработки", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 19, ParentId = 7, LevelHierarchy = 4, TitleHierarchy = "Основное подразделение", ColorHierarchy = "#7F8C8D" },
                // КИТ
                new Hierarchy { HierarchyId = 20, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Администрация", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 21, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Департамент консалтинга", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 22, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Отдел сопровождения информационных систем", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 23, ParentId = 8, LevelHierarchy = 4, TitleHierarchy = "Департамент по работе с иностранными заказчиками", ColorHierarchy = "#7F8C8D" },
                // КИТ.Р
                new Hierarchy { HierarchyId = 24, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Администрация", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 25, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Департамент разработки", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 26, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Департамент кибербезопасности", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 27, ParentId = 9, LevelHierarchy = 4, TitleHierarchy = "Департамент маркетинга", ColorHierarchy = "#7F8C8D" },
                // Сайберлимфа
                new Hierarchy { HierarchyId = 28, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел разработки", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 29, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Коммерческий департамент", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 30, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Лаборатория кибербезопасности", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 31, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Администрация", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 32, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел персонала", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 33, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел технического сопровождения", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 34, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел интеграции и автоматизации", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 35, ParentId = 10, LevelHierarchy = 4, TitleHierarchy = "Отдел продуктового менеджмента", ColorHierarchy = "#7F8C8D" },
                // Витропс
                new Hierarchy { HierarchyId = 36, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Администрация", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 37, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Планово-экономический отдел", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 38, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Отдел поддержки 1С", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 39, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Отдел кадрового делопроизводства", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 40, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Юридический отдел", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 41, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Служба внутреннего сервиса", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 42, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Отдел делопроизводства", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 43, ParentId = 11, LevelHierarchy = 4, TitleHierarchy = "Бухгалтерия", ColorHierarchy = "#7F8C8D" },

                // ФТ-СОФТ - Основное подразделение
                new Hierarchy { HierarchyId = 44, ParentId = 19, LevelHierarchy = 5, TitleHierarchy = "Направление Аналитики и документации", ColorHierarchy = "#95A5A6" },
                // Сайберлимфа - отдел разработки
                new Hierarchy { HierarchyId = 45, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа серверной разработки", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 46, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа веб разработки", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 47, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа аналитики", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 48, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа администрирования проектов", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 49, ParentId = 28, LevelHierarchy = 5, TitleHierarchy = "Группа продуктового дизайна", ColorHierarchy = "#7F8C8D" },
                // Сайберлимфа - Коммерческий департамент
                new Hierarchy { HierarchyId = 50, ParentId = 29, LevelHierarchy = 5, TitleHierarchy = "Отдел маркетинга", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 51, ParentId = 29, LevelHierarchy = 5, TitleHierarchy = "Отдел технической поддержки продаж", ColorHierarchy = "#7F8C8D" },
                // Сайберлимфа - Лаборатория кибербезопасности
                new Hierarchy { HierarchyId = 52, ParentId = 30, LevelHierarchy = 5, TitleHierarchy = "Производственное направление", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 53, ParentId = 30, LevelHierarchy = 5, TitleHierarchy = "Аналитическое направление", ColorHierarchy = "#7F8C8D" },
                new Hierarchy { HierarchyId = 54, ParentId = 30, LevelHierarchy = 5, TitleHierarchy = "Отдел документирования и локализации", ColorHierarchy = "#7F8C8D" },
            };
        }

        private List<User> CreateUsersWithHierarchy(List<Hierarchy> hierarchies)
        {
            var ceo = CreateTestUser(Guid.NewGuid(), "CEO", "ФТ-СОФТ", "Руководитель", "Management", hierarchyId: 44);
            var dev1 = CreateTestUser(Guid.NewGuid(), "Разработчик", "1", "Разработчик", "IT", hierarchyId: 44, managerId: ceo.User_id);
            var dev2 = CreateTestUser(Guid.NewGuid(), "Разработчик", "2", "Разработчик", "IT", hierarchyId: 44, managerId: ceo.User_id);

            return new List<User> { ceo, dev1, dev2 };
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
                HierarchyId = hierarchyId
            };
        }

        private HierarchyNodeDto FindNodeByTitle(HierarchyNodeDto node, string title)
        {
            if (node.Title == title)
                return node;

            foreach (var child in node.Children)
            {
                var found = FindNodeByTitle(child, title);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}

