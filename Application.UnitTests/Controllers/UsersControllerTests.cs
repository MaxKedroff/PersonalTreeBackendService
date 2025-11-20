using Application.Interfaces;
using Castle.Core.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.Controllers;
using Microsoft.Extensions.Logging;
using Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Application.Validators;

namespace Application.UnitTests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<UsersController>> _mockLogger;
        private readonly UsersController _controller;
        private readonly TableRequestDtoValidator _validator;


        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<UsersController>>();
            _validator = new TableRequestDtoValidator();
            _controller = new UsersController(_mockUserService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetUsers_ValidRequest_ReturnsOkResult()
        {
            var expectedResponse = new ResponseTableUsersDto
            {
                AmountOfUsers = 1,
                UsersTable = new List<TableUserDto>
                {
                    new TableUserDto { UserId = Guid.NewGuid(), UserName = "Test User" }
                },
                CurrentPage = 1,
                TotalPages = 1,
                PageSize = 10
            };

            _mockUserService.Setup(x => x.GetUserTableAsync(It.IsAny<TableRequestDto>())).ReturnsAsync(expectedResponse);

            var result = await _controller.GetUsers();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ResponseTableUsersDto>(okResult.Value);
            Assert.Equal(expectedResponse.AmountOfUsers, response.AmountOfUsers);
        }

        [Fact]
        public void GetUsers_InvalidPage_ReturnsValidationError()
        {
            var invalidDto = new TableRequestDto { page = 0, Limit = 10 };
            var validationResult = _validator.Validate(invalidDto);

            Assert.False(validationResult.IsValid);
            Assert.Contains(validationResult.Errors,
                error => error.PropertyName == "page" &&
                        error.ErrorMessage == "Номер страницы должен быть не меньше 1.");
        }


        [Fact]
        public void GetUsers_InvalidLimit_ReturnsValidationError()
        {
            var invalidDto = new TableRequestDto { page = 1, Limit = 0 };

            var validationResult = _validator.Validate(invalidDto);

            Assert.False(validationResult.IsValid);
            Assert.Contains(validationResult.Errors,
                error => error.PropertyName == "Limit" &&
                        error.ErrorMessage == "Лимит должен быть от 1 до 100.");
        }

        [Fact]
        public async Task GetUserById_ValidId_ReturnsUser()
        {
            var userId = Guid.NewGuid();
            var expectedUser = new UserDetailInfoDto { User_id = userId, UserName = "Test User" };

            _mockUserService.Setup(x => x.GetUserDetailAsync(userId))
                .ReturnsAsync(expectedUser);

            var result = await _controller.GetUserById(userId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var user = Assert.IsType<UserDetailInfoDto>(okResult.Value);
            Assert.Equal(userId, user.User_id);
        }

        [Fact]
        public void UserIdValidator_EmptyGuid_ReturnsValidationError()
        {
            var validator = new UserIdValidator();
            var emptyGuid = Guid.Empty;
            var validationResult = validator.Validate(emptyGuid);
            Assert.False(validationResult.IsValid);
            Assert.Contains(validationResult.Errors,
                error => error.ErrorMessage == "User ID cannot be empty GUID");
        }

        [Fact]
        public async Task GetUserById_NotFound_ReturnsNotFound()
        {
            var userId = Guid.NewGuid();
            _mockUserService.Setup(x => x.GetUserDetailAsync(userId))
                .ThrowsAsync(new KeyNotFoundException());

            var result = await _controller.GetUserById(userId);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task SearchItems_ValidRequest_ReturnsResults()
        {
            var request = new SearchRequestDto
            {
                searchCriteria = "name",
                searchValue = "test",
                queryAmount = 10
            };

            var expectedResponse = new SearchResponseDto
            {
                amount = 1,
                searchItems = new List<SearchItemDto>
                {
                    new SearchItemDto { username = "Test User" }
                }
            };

            _mockUserService.Setup(x => x.GetSearchResultAsync(request))
                .ReturnsAsync(expectedResponse);

            var result = await _controller.SearchItems(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<SearchResponseDto>(okResult.Value);
            Assert.Equal(expectedResponse.amount, response.amount);
        }

        [Fact]
        public async Task SearchItems_NullRequest_ReturnsBadRequest()
        {
            var result = await _controller.SearchItems(null);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task SearchItems_ShortSearchValue_ReturnsBadRequest()
        {
            var request = new SearchRequestDto { searchValue = "a" };
            var result = await _controller.SearchItems(request);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetDepartmentHierarchy_ValidData_ReturnsHierarchy()
        {
            var expectedHierarchy = new HierarchyResponseDto
            {
                Ceo = new EmployeeHierarchyDto { UserName = "CEO" },
                Departments = new List<DepartmentHierarchyDto>(),
                TotalEmployees = 1
            };

            _mockUserService.Setup(x => x.GetDepartmentHierarchyAsync())
                .ReturnsAsync(expectedHierarchy);
            var result = await _controller.GetDepartmentHierarchy();
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var hierarchy = Assert.IsType<HierarchyResponseDto>(okResult.Value);
            Assert.Equal(expectedHierarchy.TotalEmployees, hierarchy.TotalEmployees);
        }

        [Fact]
        public async Task GetDepartmentHierarchy_EmptyHierarchy_ReturnsNotFound()
        {
            var emptyHierarchy = new HierarchyResponseDto
            {
                Ceo = null,
                Departments = new List<DepartmentHierarchyDto>(),
                TotalEmployees = 0
            };

            _mockUserService.Setup(x => x.GetDepartmentHierarchyAsync())
                .ReturnsAsync(emptyHierarchy);
            var result = await _controller.GetDepartmentHierarchy();
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }
    }
}
