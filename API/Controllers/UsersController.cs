using Application.Dtos;
using Application.Interfaces;
using Core.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthOptions.POLICY_USER)]
    public class UsersController : ControllerBase
    {

        IUserService _userService;
        private readonly ILogger<UsersController> _logger;


        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(AuthOptions.POLICY_USER)]
        public async Task<ActionResult<ResponseTableUsersDto>> GetUsers([FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? sort = null,
            [FromQuery] string? positionFilter = null,
            [FromQuery] string? departmentFilter = null,
            [FromQuery] bool isCached = false,
            [FromQuery] string SearchText = null)
        {
            try
            {
                _logger.LogInformation("Getting users table - Page: {Page}, Limit: {Limit}, Sort: {Sort}",
                    page, limit, sort);

                var request = new TableRequestDto
                {
                    page = page,
                    Limit = limit,
                    Sort = sort,
                    PositionFilter = positionFilter,
                    DepartmentFilter = departmentFilter,
                    isCached = isCached,
                    SearchText = SearchText
                };

                var result = await _userService.GetUserTableAsync(request);
                _logger.LogInformation("Retrieved {Count} users successfully", result.UsersTable?.Count);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users table");
                return StatusCode(500, new { message = "An error occurred while retrieving users" });
            }
        }

        [HttpGet("{userId}")]
        [Authorize(AuthOptions.POLICY_USER)]
        public async Task<ActionResult<UserDetailInfoDto>> GetUserById(Guid userId)
        {
            try
            {
                _logger.LogInformation("Getting user details for ID: {UserId}", userId);

                var result = await _userService.GetUserDetailAsync(userId);

                if (result == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return NotFound(new { message = "User not found" });
                }

                _logger.LogInformation("User details retrieved successfully for ID: {UserId}", userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found with ID: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user details for ID: {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving user details" });
            }
        }

        [HttpPost("search")]
        [Authorize(AuthOptions.POLICY_USER)]
        [Obsolete("Use GetUserTableAsync with search functionality instead")]
        public async Task<ActionResult<SearchResponseDto>> SearchItems([FromBody] SearchRequestDto request)
        {
            try
            {
                _logger.LogInformation("Search request - Criteria: {Criteria}, Value: {Value}",
                    request?.searchCriteria, request?.searchValue);

                if (request == null)
                {
                    _logger.LogWarning("Search request is null");
                    return BadRequest(new { message = "Search request cannot be null" });
                }

                if (string.IsNullOrWhiteSpace(request.searchValue))
                {
                    _logger.LogWarning("Empty search value provided");
                    return BadRequest(new { message = "Search value cannot be empty" });
                }

                if (request.searchValue.Length < 2)
                {
                    _logger.LogWarning("Search value too short: {Value}", request.searchValue);
                    return BadRequest(new { message = "Search value must be at least 2 characters long" });
                }

                var result = await _userService.GetSearchResultAsync(request);
                _logger.LogInformation("Search completed - Found {Count} results", result.amount);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in search request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during search");
                return StatusCode(500, new { message = "An error occurred during search" });
            }
        }

        [HttpGet("hierarchy")]
        [Authorize(AuthOptions.POLICY_USER)]
        [ProducesResponseType(typeof(HierarchyResponseDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<HierarchyResponseDto>> GetDepartmentHierarchy()
        {
            try
            {
                _logger.LogInformation("Getting department hierarchy");

                var hierarchy = await _userService.GetDepartmentHierarchyAsync();

                if (hierarchy?.Ceo == null && hierarchy?.Departments?.Count == 0)
                {
                    _logger.LogWarning("No organizational hierarchy found");
                    return NotFound(new { message = "No organizational hierarchy found" });
                }

                _logger.LogInformation("Hierarchy retrieved successfully - Total employees: {Count}",
                    hierarchy.TotalEmployees);

                return Ok(hierarchy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving organizational hierarchy");
                return StatusCode(500, new { message = "An error occurred while retrieving organizational hierarchy" });
            }
        }

        [HttpPut("{userId}")]
        [Authorize(AuthOptions.POLICY_USER)]
        public async Task<ActionResult<UserDetailInfoDto>> UpdateProfile(Guid userId, [FromBody] UpdateProfileDto updateDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                _logger.LogInformation("Update profile request - Target: {UserId}, Current User: {CurrentUserId}, Role: {Role}",
                    userId, currentUserId, currentUserRole);

                var updatedUser = await _userService.UpdateUserProfileAsync(userId, currentUserId, currentUserRole, updateDto);

                return Ok(updatedUser);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized profile update attempt - User: {UserId}, Current User: {CurrentUserId}",
                    userId, GetCurrentUserId());
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("User not found for update: {UserId}", userId);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid argument in profile update: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user: {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while updating profile" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            return userId;
        }

        private string GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleClaim))
            {
                throw new UnauthorizedAccessException("Role claim not found in token");
            }
            return roleClaim;
        }
    }
}
