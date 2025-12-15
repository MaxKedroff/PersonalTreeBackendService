using Application.Dtos;
using Application.Interfaces;
using Application.Utils;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        [Obsolete("Use GetUserTableAsync with search functionality instead")]
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
            var cacheKey = $"user_table_{request.page}_{request.Limit}_{request.Sort}_{request.PositionFilter}_{request.DepartmentFilter}_{request.SearchText}";

            _logger.LogInformation("Getting users table - Page: {Page}, Limit: {Limit}, " +
                                "PositionFilter: '{PositionFilter}', DepartmentFilter: '{DepartmentFilter}', " +
                                "SearchText: '{SearchText}', Sort: '{Sort}', IsCached: {IsCached}",
                                request.page, request.Limit, request.PositionFilter,
                                request.DepartmentFilter, request.SearchText, request.Sort, request.isCached);
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
                    departmentFilter: request.DepartmentFilter,
                    searchText: request.SearchText
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

                response.UsersTable.Select(async user => user.hierarchyColor = await _userRepository.GetColorByTitleHierarchy(user.Department));

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
                                   "DepartmentFilter: '{DepartmentFilter}', SearchText: '{SearchText}'",
                                   request.page, request.Limit, request.PositionFilter,
                                   request.DepartmentFilter, request.SearchText);
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

        public async Task<UserDetailInfoDto> UpdateUserProfileAsync(Guid userId, Guid currentUserId, string currentUserRole, UpdateProfileDto updateDto)
        {
            _logger.LogInformation("Updating user profile - Target User: {UserId}, Current User: {CurrentUserId}, Role: {Role}",
                userId, currentUserId, currentUserRole);

            try
            {
                if (userId != currentUserId && currentUserRole != "Admin" && currentUserRole != "Hr")
                {
                    _logger.LogWarning("User {CurrentUserId} with role {Role} attempted to update profile of user {UserId} without permission",
                        currentUserId, currentUserRole, userId);
                    throw new UnauthorizedAccessException("You don't have permission to update this user's profile");
                }

                var user = await _userRepository.GetUsersByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for update: {UserId}", userId);
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                if (!string.IsNullOrWhiteSpace(updateDto.Phone))
                {
                    user.ContactInfo.Phone = updateDto.Phone;
                    _logger.LogDebug("Updated phone for user {UserId}", userId);
                }

                if (!string.IsNullOrWhiteSpace(updateDto.City))
                {
                    user.ContactInfo.City = updateDto.City;
                    _logger.LogDebug("Updated city for user {UserId}", userId);
                }

                if (!string.IsNullOrWhiteSpace(updateDto.Interests))
                {
                    user.PersonalInfo.Interests = updateDto.Interests;
                    _logger.LogDebug("Updated interests for user {UserId}", userId);
                }

                if (!string.IsNullOrWhiteSpace(updateDto.Avatar))
                {
                    if (updateDto.Avatar.StartsWith("data:image/") ||
                        updateDto.Avatar.StartsWith("http://") ||
                        updateDto.Avatar.StartsWith("https://"))
                    {
                        user.ContactInfo.Avatar = updateDto.Avatar;
                        _logger.LogDebug("Updated avatar for user {UserId}", userId);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid avatar format for user {UserId}", userId);
                        throw new ArgumentException("Invalid avatar format. Expected Base64 data URL or HTTP URL");
                    }
                }

                if (!string.IsNullOrEmpty(updateDto.Position) && currentUserRole == "Admin")
                {
                    user.WorkInfo.Position = updateDto.Position;
                    _logger.LogDebug("Updated Position for user {UserId}", userId);
                }

                if (!string.IsNullOrEmpty(updateDto.Department) && currentUserRole == "Admin")
                {
                    user.WorkInfo.Department = updateDto.Department;
                    _logger.LogDebug("Updated Position for user {UserId}", userId);
                }

                if (updateDto.Contacts != null && updateDto.Contacts.Any())
                {
                    foreach (var contact in updateDto.Contacts)
                    {
                        if (!string.IsNullOrWhiteSpace(contact.Key))
                        {
                            user.SetContact(contact.Key, contact.Value);
                            _logger.LogDebug("Updated contact {ContactKey} for user {UserId}", contact.Key, userId);
                        }
                    }
                }

                user.Updated_at = DateTime.UtcNow;

                await _userRepository.UpdateUserAsync(user);

                _logger.LogInformation("User profile updated successfully - User: {UserId}, Updated by: {CurrentUserId}",
                    userId, currentUserId);


                return Mapper.MapUserToUserDetailInfoDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user profile for ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<HierarchyNodeDto> GetDepartmentHierarchyAsyncV2()
        {
            var hierarchies = await _userRepository.GetHierarchiesList();
            var users = await _userRepository.GetUsersWithHierarchyV2Async();

            var hierarchyDict = hierarchies.ToDictionary(h => h.HierarchyId, h => new HierarchyNodeDto
            {
                HierarchyId = h.HierarchyId,
                Level = h.LevelHierarchy,
                Title = h.TitleHierarchy,
                Color = h.ColorHierarchy,
                Children = new List<HierarchyNodeDto>()
            });

            var root = hierarchyDict.Values.FirstOrDefault(h => h.Level == 1);
            if (root == null)
            {
                root = new HierarchyNodeDto { Level = 1, Title = "UDV GROUP", Color = "#000000" };
                hierarchyDict[-1] = root;
            }

            foreach (var h in hierarchies.Where(h => h.ParentId.HasValue))
            {
                if (hierarchyDict.TryGetValue(h.ParentId.Value, out var parent) &&
            hierarchyDict.TryGetValue(h.HierarchyId, out var child))
                {
                    parent.Children.Add(child);
                }
            }

            foreach (var node in hierarchyDict.Values)
            {
                if (node.Level >= 4 && node.Children.Count == 0) 
                {
                    var usersInNode = users
                        .Where(u => u.HierarchyId.HasValue && u.HierarchyId.Value == node.HierarchyId)
                        .ToList();

                    if (!usersInNode.Any()) continue;

                    var ceo = usersInNode.FirstOrDefault(u =>
                        !u.Manager_id.HasValue ||
                        !usersInNode.Any(m => m.User_id == u.Manager_id));

                    if (ceo != null)
                    {
                        node.Manager = Mapper.MapEmployeeToHierarchyDto(ceo, usersInNode);
                        var subordinates = usersInNode.Where(u => u.Manager_id == ceo.User_id).ToList();
                        node.Employees = usersInNode
                            .Where(u => u.User_id != ceo.User_id)
                            .Select(u => Mapper.MapEmployeeToFlatDto(u)) 
                            .ToList();

                    }
                    else
                    {
                        var first = usersInNode.First();
                        node.Manager = Mapper.MapEmployeeToHierarchyDto(first, usersInNode);
                        node.Employees = usersInNode
                            .Where(u => u.User_id != first.User_id)
                            .Select(u => Mapper.MapEmployeeToFlatDto(u))
                            .ToList();
                    }
                }
            }
            return root;
        }

        public async Task<UserDetailInfoDto> MoveUserToHierarchyAsync(MoveUserRequestDto moveRequest, Guid currentUserId, string currentUserRole)
        {
            _logger.LogInformation("Moving user to hierarchy - User: {UserId}, TargetHierarchy: {TargetHierarchyId}, SwapWith: {SwapWithUserId}, BecomeCeo: {BecomeCeo}",
                moveRequest.UserId, moveRequest.TargetHierarchyId, moveRequest.SwapWithUserId, moveRequest.BecomeCeo);

            try
            {
                if (currentUserRole != "Admin" && currentUserRole != "Hr")
                {
                    _logger.LogWarning("User {CurrentUserId} with role {Role} attempted to move user without permission",
                        currentUserId, currentUserRole);
                    throw new UnauthorizedAccessException("You don't have permission to move users");
                }

                var userToMove = await _userRepository.GetUsersByIdAsync(moveRequest.UserId);
                if (userToMove == null)
                {
                    _logger.LogWarning("User not found for move: {UserId}", moveRequest.UserId);
                    throw new KeyNotFoundException($"User with ID {moveRequest.UserId} not found");
                }

                var targetHierarchy = await _userRepository.GetHierarchyByIdAsync(moveRequest.TargetHierarchyId);
                if (targetHierarchy == null)
                {
                    _logger.LogWarning("Target hierarchy not found: {HierarchyId}", moveRequest.TargetHierarchyId);
                    throw new KeyNotFoundException($"Target hierarchy with ID {moveRequest.TargetHierarchyId} not found");
                }

                if (targetHierarchy.LevelHierarchy < 4 || targetHierarchy.LevelHierarchy > 5)
                {
                    _logger.LogWarning("Cannot move user to non-leaf hierarchy level: {Level}", targetHierarchy.LevelHierarchy);
                    throw new InvalidOperationException("Can only move users to leaf hierarchies (levels 4-5)");
                }

                if (moveRequest.BecomeCeo)
                {
                    if (userToMove.HierarchyId != targetHierarchy.HierarchyId)
                    {
                        throw new InvalidOperationException("Must be in the target department to become CEO");
                    }
                    await HandleBecomeCeoScenario(userToMove, targetHierarchy);
                }
                else if (moveRequest.SwapWithUserId.HasValue)
                {
                    await HandleSwapScenario(userToMove, moveRequest.SwapWithUserId.Value, targetHierarchy);
                }
                else
                {
                    await HandleRegularMoveScenario(userToMove, moveRequest.NewManagerId, targetHierarchy);
                }


                _logger.LogInformation("User moved successfully - User: {UserId}, TargetHierarchy: {TargetHierarchyId}",
                    moveRequest.UserId, moveRequest.TargetHierarchyId);

                return Mapper.MapUserToUserDetailInfoDto(userToMove);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while moving user {UserId} to hierarchy {TargetHierarchyId}",
                    moveRequest.UserId, moveRequest.TargetHierarchyId);
                throw;
            }
        }

        private async Task HandleRegularMoveScenario(User userToMove, Guid? newManagerId, Hierarchy targetHierarchy)
        {
            _logger.LogInformation("Handling regular move scenario - User: {UserId}", userToMove.User_id);

            if (userToMove.Subordinates?.Any() == true && userToMove.Manager_id == null)
            {
                _logger.LogWarning("Cannot move CEO user with subordinates: {UserId}", userToMove.User_id);
                throw new InvalidOperationException("Cannot move CEO user with subordinates. Use SWAP instead.");
            }

            Guid? actualManagerId = newManagerId;
            if (!actualManagerId.HasValue)
            {
                var targetHierarchyCeo = await _userRepository.GetCeoByHierarchyIdAsync(targetHierarchy.HierarchyId);
                if (targetHierarchyCeo != null)
                {
                    actualManagerId = targetHierarchyCeo.User_id;
                    _logger.LogDebug("Auto-assigned CEO as manager: {ManagerId}", actualManagerId);
                }
            }
            else
            {
                var specifiedManager = await _userRepository.GetUsersByIdAsync(actualManagerId.Value);
                if (specifiedManager?.HierarchyId != targetHierarchy.HierarchyId)
                {
                    throw new InvalidOperationException("Specified manager must be in the target hierarchy");
                }
            }

            userToMove.HierarchyId = targetHierarchy.HierarchyId;
            userToMove.Manager_id = actualManagerId;
            userToMove.Updated_at = DateTime.UtcNow;

            var manager = await _userRepository.GetUsersByIdAsync(actualManagerId.Value);
            var managerDepartment = manager.WorkInfo.Department;

            if (userToMove.WorkInfo != null)
            {
                userToMove.WorkInfo.Department = managerDepartment;
            }

            await _userRepository.UpdateUserAsync(userToMove);

            _logger.LogInformation("Regular move completed - User: {UserId}, Manager: {ManagerId}",
                userToMove.User_id, actualManagerId);
        }

        private async Task HandleSwapScenario(User userToMove, Guid swapWithUserId, Hierarchy targetHierarchy)
        {
            _logger.LogInformation("Handling SWAP scenario - User: {UserId}, SwapWith: {SwapWithUserId}",
                userToMove.User_id, swapWithUserId);

            var swapWithUser = await _userRepository.GetUsersByIdAsync(swapWithUserId);
            if (swapWithUser == null)
            {
                throw new KeyNotFoundException($"User to swap with (ID: {swapWithUserId}) not found");
            }

            bool areRelated = userToMove.Manager_id == swapWithUserId ||
                             swapWithUser.Manager_id == userToMove.User_id ||
                             userToMove.Manager_id == swapWithUser.Manager_id;

            if (!areRelated)
            {
                throw new InvalidOperationException("Can only swap with manager or subordinate");
            }

            var tempHierarchyId = userToMove.HierarchyId;
            var tempManagerId = userToMove.Manager_id;
            var tempDepartment = userToMove.WorkInfo?.Department;
            var tempSubordinates = userToMove.Subordinates?.ToList() ?? new List<User>();

            var tempSwapHierarchyId = swapWithUser.HierarchyId;
            var tempSwapManagerId = swapWithUser.Manager_id;
            var tempSwapDepartment = swapWithUser.WorkInfo?.Department;
            var tempSwapSubordinates = swapWithUser.Subordinates?.ToList() ?? new List<User>();

            userToMove.HierarchyId = tempSwapHierarchyId;
            userToMove.Manager_id = tempSwapManagerId;
            userToMove.Updated_at = DateTime.UtcNow;

            if (userToMove.WorkInfo != null)
            {
                userToMove.WorkInfo.Department = swapWithUser.WorkInfo?.Department ?? targetHierarchy.TitleHierarchy;
            }

            swapWithUser.HierarchyId = tempHierarchyId;
            swapWithUser.Manager_id = tempManagerId;
            swapWithUser.Updated_at = DateTime.UtcNow;

            if (swapWithUser.WorkInfo != null)
            {
                swapWithUser.WorkInfo.Department = tempDepartment ?? swapWithUser.WorkInfo.Department;
            }

            await RedistributeSubordinatesAfterSwap(userToMove, swapWithUser, tempSubordinates, tempSwapSubordinates);

            await _userRepository.UpdateUserAsync(userToMove);
            await _userRepository.UpdateUserAsync(swapWithUser);

            _logger.LogInformation("SWAP completed successfully - Users swapped: {User1} and {User2}",
                userToMove.User_id, swapWithUserId);
        }
        private async Task HandleBecomeCeoScenario(User userToMove, Hierarchy targetHierarchy)
        {
            _logger.LogInformation("Handling Become CEO scenario - User: {UserId}, TargetHierarchy: {HierarchyId}",
                userToMove.User_id, targetHierarchy.HierarchyId);

            if (userToMove.HierarchyId != targetHierarchy.HierarchyId)
            {
                throw new InvalidOperationException("Can only become CEO within current department. Move to department first.");
            }

            var currentCeo = await _userRepository.GetCeoByHierarchyIdAsync(targetHierarchy.HierarchyId);

            if (currentCeo == null)
            {
                throw new InvalidOperationException("No CEO found in the target department");
            }

            bool isSubordinate = userToMove.Manager_id == currentCeo.User_id;
            if (!isSubordinate)
            {
                throw new InvalidOperationException("Can only become CEO if you are subordinate of current CEO");
            }

            _logger.LogInformation("Performing CEO promotion SWAP - User: {UserId}, CurrentCEO: {CurrentCeoId}",
                userToMove.User_id, currentCeo.User_id);

            var userSubordinates = userToMove.Subordinates?.ToList() ?? new List<User>();
            var ceoSubordinates = currentCeo.Subordinates?
                .Where(s => s.User_id != userToMove.User_id)
                .ToList() ?? new List<User>();

            userToMove.Manager_id = null; 
            userToMove.Updated_at = DateTime.UtcNow;

            currentCeo.Manager_id = userToMove.User_id;
            currentCeo.Updated_at = DateTime.UtcNow;

            foreach (var subordinate in ceoSubordinates)
            {
                subordinate.Manager_id = userToMove.User_id;
                subordinate.Updated_at = DateTime.UtcNow;
                await _userRepository.UpdateUserAsync(subordinate);
                _logger.LogDebug("Reassigned CEO's subordinate {SubordinateId} to new CEO {NewCeoId}",
                    subordinate.User_id, userToMove.User_id);
            }

            foreach (var subordinate in userSubordinates)
            {
                subordinate.Manager_id = currentCeo.User_id;
                subordinate.Updated_at = DateTime.UtcNow;
                await _userRepository.UpdateUserAsync(subordinate);
                _logger.LogDebug("Reassigned user's subordinate {SubordinateId} to former CEO {FormerCeoId}",
                    subordinate.User_id, currentCeo.User_id);
            }

            await _userRepository.UpdateUserAsync(userToMove);
            await _userRepository.UpdateUserAsync(currentCeo);

            _logger.LogInformation("CEO promotion completed - New CEO: {NewCeoId}, Former CEO: {FormerCeoId}",
                userToMove.User_id, currentCeo.User_id);
        }


        private async Task RedistributeSubordinatesAfterSwap(User user1, User user2,
    List<User> user1OriginalSubordinates, List<User> user2OriginalSubordinates)
        {
            _logger.LogInformation("Redistributing subordinates after SWAP - User1: {User1Id}, User2: {User2Id}",
                user1.User_id, user2.User_id);

            if (user2OriginalSubordinates.Any(s => s.User_id == user1.User_id))
            {
                _logger.LogDebug("User1 was subordinate of User2 - handling subordinate-manager swap");

                foreach (var subordinate in user2OriginalSubordinates.Where(s => s.User_id != user1.User_id))
                {
                    subordinate.Manager_id = user1.User_id;
                    subordinate.HierarchyId = user1.HierarchyId;
                    subordinate.Updated_at = DateTime.UtcNow;
                    if (subordinate.WorkInfo != null)
                    {
                        subordinate.WorkInfo.Department = user1.WorkInfo?.Department ?? subordinate.WorkInfo.Department;
                    }
                    await _userRepository.UpdateUserAsync(subordinate);
                    _logger.LogDebug("Reassigned subordinate {SubordinateId} from {OldManager} to {NewManager}",
                        subordinate.User_id, user2.User_id, user1.User_id);
                }

                user2.Manager_id = user1.User_id;

                foreach (var subordinate in user1OriginalSubordinates)
                {
                    subordinate.HierarchyId = user1.HierarchyId;
                    subordinate.Updated_at = DateTime.UtcNow;
                    if (subordinate.WorkInfo != null)
                    {
                        subordinate.WorkInfo.Department = user1.WorkInfo?.Department ?? subordinate.WorkInfo.Department;
                    }
                    await _userRepository.UpdateUserAsync(subordinate);
                    _logger.LogDebug("Updated subordinate {SubordinateId} department for user1",
                        subordinate.User_id);
                }
            }
            else if (user1OriginalSubordinates.Any(s => s.User_id == user2.User_id))
            {
                _logger.LogDebug("User2 was subordinate of User1 - handling manager-subordinate swap");

                foreach (var subordinate in user1OriginalSubordinates.Where(s => s.User_id != user2.User_id))
                {
                    subordinate.Manager_id = user2.User_id;
                    subordinate.HierarchyId = user2.HierarchyId;
                    subordinate.Updated_at = DateTime.UtcNow;
                    if (subordinate.WorkInfo != null)
                    {
                        subordinate.WorkInfo.Department = user2.WorkInfo?.Department ?? subordinate.WorkInfo.Department;
                    }
                    await _userRepository.UpdateUserAsync(subordinate);
                    _logger.LogDebug("Reassigned subordinate {SubordinateId} from {OldManager} to {NewManager}",
                        subordinate.User_id, user1.User_id, user2.User_id);
                }

                user1.Manager_id = user2.User_id;

                foreach (var subordinate in user2OriginalSubordinates)
                {
                    subordinate.HierarchyId = user2.HierarchyId;
                    subordinate.Updated_at = DateTime.UtcNow;
                    if (subordinate.WorkInfo != null)
                    {
                        subordinate.WorkInfo.Department = user2.WorkInfo?.Department ?? subordinate.WorkInfo.Department;
                    }
                    await _userRepository.UpdateUserAsync(subordinate);
                    _logger.LogDebug("Updated subordinate {SubordinateId} department for user2",
                        subordinate.User_id);
                }
            }
            else if (user1.Manager_id == user2.Manager_id)
            {
                _logger.LogDebug("Users were siblings - exchanging subordinates between departments");

                foreach (var subordinate in user1OriginalSubordinates)
                {
                    subordinate.HierarchyId = user2.HierarchyId;
                    subordinate.Updated_at = DateTime.UtcNow;
                    if (subordinate.WorkInfo != null)
                    {
                        subordinate.WorkInfo.Department = user2.WorkInfo?.Department ?? subordinate.WorkInfo.Department;
                    }
                    await _userRepository.UpdateUserAsync(subordinate);
                    _logger.LogDebug("Updated subordinate {SubordinateId} department to {NewDepartment}",
                        subordinate.User_id, user2.HierarchyId);
                }

                foreach (var subordinate in user2OriginalSubordinates)
                {
                    subordinate.HierarchyId = user1.HierarchyId;
                    subordinate.Updated_at = DateTime.UtcNow;
                    if (subordinate.WorkInfo != null)
                    {
                        subordinate.WorkInfo.Department = user1.WorkInfo?.Department ?? subordinate.WorkInfo.Department;
                    }
                    await _userRepository.UpdateUserAsync(subordinate);
                    _logger.LogDebug("Updated subordinate {SubordinateId} department to {NewDepartment}",
                        subordinate.User_id, user1.HierarchyId);
                }
            }
            else
            {
                _logger.LogDebug("Other swap scenario - subordinates remain with their current managers");
            }

            _logger.LogInformation("Subordinate redistribution completed");
        }

        public async Task<HierarchyNodeWithoutPersonsDto> GetDepartmentTreeAsync()
        {
            var hierarchies = await _userRepository.GetHierarchiesList();

            var hierarchyDict = hierarchies.ToDictionary(h => h.HierarchyId, h => new HierarchyNodeWithoutPersonsDto
            {
                HierarchyId = h.HierarchyId,
                Level = h.LevelHierarchy,
                Title = h.TitleHierarchy,
                Color = h.ColorHierarchy,
                Children = new List<HierarchyNodeWithoutPersonsDto>()
            });

            var root = hierarchyDict.Values.FirstOrDefault(h => h.Level == 1);
            if (root == null)
            {
                root = new HierarchyNodeWithoutPersonsDto { Level = 1, Title = "UDV GROUP", Color = "#000000" };
                hierarchyDict[-1] = root;
            }

            foreach (var h in hierarchies.Where(h => h.ParentId.HasValue))
            {
                if (hierarchyDict.TryGetValue(h.ParentId.Value, out var parent) &&
            hierarchyDict.TryGetValue(h.HierarchyId, out var child))
                {
                    parent.Children.Add(child);
                }
            }

            
            return root;
        }

        public async Task<DepartmentDetailsDto> GetDetailsFromDepartment(string hierarchyId)
        {
            if (!int.TryParse(hierarchyId, out var hierarchyIdInt))
            {
                throw new ArgumentException("Invalid hierarchy ID format", nameof(hierarchyId));
            }

            var hierarchies = await _userRepository.GetHierarchiesList();
            var targetHierarchy = hierarchies.FirstOrDefault(h => h.HierarchyId == hierarchyIdInt);

            if (targetHierarchy == null)
            {
                throw new KeyNotFoundException($"Hierarchy with ID {hierarchyId} not found");
            }

            var users = await _userRepository.GetUsersWithHierarchyV2Async();

            var usersInDepartment = users
                .Where(u => u.HierarchyId.HasValue && u.HierarchyId.Value == hierarchyIdInt)
                .ToList();

            EmployeeHierarchyDto? manager = null;
            List<EmployeeFlatDto> employees = new List<EmployeeFlatDto>();

            if (usersInDepartment.Any())
            {
                var ceo = usersInDepartment.FirstOrDefault(u =>
                    !u.Manager_id.HasValue ||
                    !usersInDepartment.Any(m => m.User_id == u.Manager_id));

                if (ceo != null)
                {
                    manager = Mapper.MapEmployeeToHierarchyDto(ceo, usersInDepartment);

                    employees = usersInDepartment
                        .Where(u => u.User_id != ceo.User_id)
                        .Select(u => Mapper.MapEmployeeToFlatDto(u))
                        .ToList();
                }
                else
                {
                    var firstUser = usersInDepartment.First();
                    manager = Mapper.MapEmployeeToHierarchyDto(firstUser, usersInDepartment);

                    employees = usersInDepartment
                        .Where(u => u.User_id != firstUser.User_id)
                        .Select(u => Mapper.MapEmployeeToFlatDto(u))
                        .ToList();
                }
            }

            var result = new DepartmentDetailsDto
            {
                HierarchyId = hierarchyIdInt,
                Title = targetHierarchy.TitleHierarchy,
                Manager = manager,
                Employees = employees
            };

            return result;
        }
    }
}
