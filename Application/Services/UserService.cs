using Application.Dtos;
using Application.Interfaces;
using Application.Utils;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<UserService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _cacheOptions;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger, IMemoryCache memoryCache)
        {
            _userRepository = userRepository;
            _logger = logger;
            _memoryCache = memoryCache;

            _cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                .SetPriority(CacheItemPriority.Normal)
                .SetSize(1);
                
        }

        public async Task<HierarchyResponseDto> GetDepartmentHierarchyAsync()
        {
            const string cacheKey = "department_hierarchy";

            _logger.LogInformation("Starting to build department hierarchy");

            try
            {
                //if (_memoryCache.TryGetValue(cacheKey, out HierarchyResponseDto cachedHierarchy))
                //{
                //    _logger.LogInformation("Department hierarchy found in cache");
                //    return cachedHierarchy;
                //}
                _logger.LogInformation("Department hierarchy not found in cache, building from database");
                var ceo = await _userRepository.GetCeoAsync();
                var allUsers = await _userRepository.GetUsersWithHierarchyAsync();

                _logger.LogInformation("Retrieved {UserCount} users for hierarchy, CEO found: {CeoFound}",
                     allUsers.Count, ceo != null);

                var response = new HierarchyResponseDto();

                if (ceo != null)
                {
                    response.Ceo = Mapper.MapEmployeeToHierarchyDto(ceo, allUsers);
                    _logger.LogDebug("Mapped CEO: {CeoName}", ceo.GetFullName());
                }
                else
                {
                    _logger.LogWarning("CEO not found in the organization");

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

                _memoryCache.Set(cacheKey, response, _cacheOptions);
                _logger.LogInformation("Department hierarchy built and cached successfully - {DepartmentCount} departments, {TotalEmployees} total employees",
                   departments.Count, allUsers.Count);
                return response;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while building department hierarchy");

                throw;
            }
        }

        [Obsolete]
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
            _logger.LogInformation("Getting user details for ID: {UserId}", userId);
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Invalid user ID provided");
                throw new ArgumentException("Invalid user ID", nameof(userId));
            }
            try
            {
                var user = await _userRepository.GetUsersByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }
                _logger.LogInformation("User details retrieved successfully for ID: {UserId}, Name: {UserName}",
                    userId, user.GetFullName());
                return Mapper.MapUserToUserDetailInfoDto(user);
            }catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user details for ID: {UserId}", userId);
                throw;
            }
            
        }

        public async Task<ResponseTableUsersDto> GetUserTableAsync(TableRequestDto request)
        {
            var cacheKey = $"user_table_{request.page}_{request.Limit}_{request.Sort}_{request.PositionFilter}_{request.DepartmentFilter}";
            _logger.LogInformation("Getting users table - Page: {Page}, Limit: {Limit}, " +
                                "PositionFilter: '{PositionFilter}', DepartmentFilter: '{DepartmentFilter}', " +
                                "Sort: '{Sort}', IsCached: {IsCached}",
                                request.page, request.Limit, request.PositionFilter,
                                request.DepartmentFilter, request.Sort, request.isCached);
            try
            {
                if (!request.isCached)
                {
                    _logger.LogInformation("Hard cache reset requested for user table, removing cache key: {CacheKey}", cacheKey);
                    _memoryCache.Remove(cacheKey);
                }
                else
                {
                    if (_memoryCache.TryGetValue(cacheKey, out ResponseTableUsersDto cachedResponse))
                    {
                        _logger.LogInformation("User table found in cache for key: {CacheKey}", cacheKey);
                        return cachedResponse;
                    }
                    _logger.LogInformation("User table not found in cache for key: {CacheKey}, querying database", cacheKey);
                }
                if (request.page < 1)
                {
                    _logger.LogWarning("Invalid page number: {Page}", request.page);
                    throw new ArgumentException("Page number must be greater than 0", nameof(request.page));
                }
                if (request.Limit < 1 || request.Limit > 100)
                {
                    _logger.LogWarning("Invalid limit: {Limit}", request.Limit);
                    throw new ArgumentException("Limit must be between 1 and 100", nameof(request.Limit));
                }
                var sortParams = ParseSortParameter(request.Sort);
                _logger.LogDebug("Parsed sort parameters - Field: '{Field}', Order: '{Order}'",
                    sortParams.Field, sortParams.Order);
                var (users, totalCount) = await _userRepository.GetUsersPagedAsync(
                page: request.page,
                pageSize: request.Limit,
                sortBy: sortParams.Field,
                sortOrder: sortParams.Order,
                positionFilter: request.PositionFilter,
                departmentFilter: request.DepartmentFilter
                );
                _logger.LogInformation("Repository returned {UserCount} users, total count: {TotalCount}",
                   users.Count, totalCount);

                var pageSize = request.Limit > 0 ? request.Limit : 10;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var response = new ResponseTableUsersDto
                {
                    AmountOfUsers = totalCount,
                    UsersTable = users.Select(usr => Mapper.MapToTableUserDto(usr)).ToList(),
                    IsCached = false,
                    CurrentPage = request.page,
                    TotalPages = totalPages,
                    PageSize = pageSize
                };

                if (request.isCached != false)
                {
                    _memoryCache.Set(cacheKey, response, _cacheOptions);
                    _logger.LogInformation("User table cached successfully for key: {CacheKey}", cacheKey);
                    response.IsCached = true; 
                }

                _logger.LogInformation("Table response prepared successfully - " +
                                     "Users: {UserCount}, Total: {TotalCount}, Pages: {TotalPages}, " +
                                     "CurrentPage: {CurrentPage}, PageSize: {PageSize}",
                                     response.UsersTable.Count, response.AmountOfUsers, response.TotalPages,
                                     response.CurrentPage, response.PageSize);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users table with parameters: " +
                                   "Page: {Page}, Limit: {Limit}, PositionFilter: '{PositionFilter}', " +
                                   "DepartmentFilter: '{DepartmentFilter}'",
                                   request.page, request.Limit, request.PositionFilter, request.DepartmentFilter);
                throw;
            }
        }

        private (string Field, string Order) ParseSortParameter(string sort)
        {
            if (string.IsNullOrEmpty(sort))
            {
                _logger.LogDebug("No sort parameter provided, using default");
                return (null, "asc");
            }

            var parts = sort.Split('_');
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid sort parameter format: '{Sort}', expected format: 'field_order'", sort);
                return (null, "asc");
            }

            _logger.LogDebug("Sort parameter parsed successfully - Field: '{Field}', Order: '{Order}'",
                parts[0], parts[1]);

            return (parts[0], parts[1].ToLower());
        }


    }
}
