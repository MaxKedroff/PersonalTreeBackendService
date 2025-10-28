using Application.Dtos;
using Application.Interfaces;
using Application.Utils;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UserService : IUserService
    {

        public IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<HierarchyResponseDto> GetDepartmentHierarchyAsync()
        {
            var ceo = await _userRepository.GetCeoAsync();
            var allUsers = await _userRepository.GetUsersWithHierarchyAsync();

            var response = new HierarchyResponseDto();

            if (ceo != null)
            {
                response.Ceo = Mapper.MapEmployeeToHierarchyDto(ceo, allUsers);
            }

            var departments = allUsers
                .Where(u => u.User_id != ceo?.User_id && !string.IsNullOrEmpty(u.WorkInfo?.Department))
                .GroupBy(u => u.WorkInfo.Department)
                .Select(g => new DepartmentHierarchyDto
                {
                    Department = g.Key,
                    Employees = g.Where(u => u.Manager_id == ceo?.User_id ||
                                   !allUsers.Any(m => m.User_id == u.Manager_id && m.WorkInfo?.Department == g.Key))
                        .Select(emp => Mapper.MapEmployeeToHierarchyDto(emp, allUsers))
                        .ToList()
                }).ToList();

            response.Departments = departments;
            response.TotalEmployees = allUsers.Count;

            return response;

        }

        public async Task<SearchResponseDto> GetSearchResultAsync(SearchRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.searchValue))
                return new SearchResponseDto
                {
                    amount = 0,
                    searchItems = new List<SearchItemDto>(),
                    is_cached = false
                };

            var queryAmount = request.queryAmount > 0 ? request.queryAmount : 10;

            var users = await _userRepository.GetSearchResultAsync(request.searchCriteria, request.searchValue, request.queryAmount);
            var searchItems = users.Select(user => new SearchItemDto
            {
                username = user.GetFullName() ?? user.Login,
                department = user.WorkInfo?.Department ?? string.Empty,
                position = user.WorkInfo?.Position ?? string.Empty
            }).ToList();

            return new SearchResponseDto
            {
                amount = searchItems.Count,
                searchItems = searchItems,
                is_cached = false
            };
        }

        public async Task<UserDetailInfoDto> GetUserDetailAsync(Guid userId)
        {
            var user = await _userRepository.GetUsersByIdAsync(userId);
            return Mapper.MapUserToUserDetailInfoDto(user);
        }

        public async Task<ResponseTableUsersDto> GetUserTableAsync(TableRequestDto request)
        {
            Console.WriteLine($"=== GetUserTableAsync START ===");
            Console.WriteLine($"Request: page={request.page}, Limit={request.Limit}, " +
                             $"PositionFilter='{request.PositionFilter}', DepartmentFilter='{request.DepartmentFilter}'");

            // Парсим параметры сортировки
            var sortParams = ParseSortParameter(request.Sort);
            Console.WriteLine($"Sort: Field='{sortParams.Field}', Order='{sortParams.Order}'");

            // Получаем данные с пагинацией, фильтрацией и сортировкой
            var (users, totalCount) = await _userRepository.GetUsersPagedAsync(
                page: request.page,
                pageSize: request.Limit,
                sortBy: sortParams.Field,
                sortOrder: sortParams.Order,
                positionFilter: request.PositionFilter,
                departmentFilter: request.DepartmentFilter
            );

            Console.WriteLine($"Repository result: {users.Count} users, totalCount={totalCount}");

            // Рассчитываем общее количество страниц
            var pageSize = request.Limit > 0 ? request.Limit : 10;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var response = new ResponseTableUsersDto
            {
                AmountOfUsers = totalCount,
                UsersTable = users.Select(usr => Mapper.MapToTableUserDto(usr)).ToList(),
                IsCached = request.isCached,
                CurrentPage = request.page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            Console.WriteLine($"Final response: AmountOfUsers={response.AmountOfUsers}, UsersTable.Count={response.UsersTable.Count}");
            Console.WriteLine($"=== GetUserTableAsync END ===");

            return response;
        }

        private (string Field, string Order) ParseSortParameter(string sort)
        {
            if (string.IsNullOrEmpty(sort))
                return (null, "asc");

            var parts = sort.Split('_');
            if (parts.Length != 2)
                return (null, "asc");

            return (parts[0], parts[1].ToLower());
        }


    }
}
