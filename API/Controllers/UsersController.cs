using Application.Dtos;
using Application.Interfaces;
using Core.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            [FromQuery] bool isCached = false)
        {
            try
            {
                _logger.LogInformation("Getting users table - Page: {Page}, Limit: {Limit}, Sort: {Sort}",
                    page, limit, sort);

                if (page < 1)
                {
                    _logger.LogWarning("Invalid page number: {Page}", page);
                    return BadRequest(new { message = "Page number must be greater than 0" });
                }

                if (limit < 1 || limit > 100)
                {
                    _logger.LogWarning("Invalid limit: {Limit}", limit);
                    return BadRequest(new { message = "Limit must be between 1 and 100" });
                }

                var request = new TableRequestDto
                {
                    page = page,
                    Limit = limit,
                    Sort = sort,
                    PositionFilter = positionFilter,
                    DepartmentFilter = departmentFilter,
                    isCached = isCached
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

                if (userId == Guid.Empty)
                {
                    _logger.LogWarning("Invalid user ID provided");
                    return BadRequest(new { message = "Invalid user ID" });
                }

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
    }
}
