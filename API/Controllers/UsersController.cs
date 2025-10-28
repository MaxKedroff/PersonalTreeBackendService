using Application.Dtos;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {

        IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<ResponseTableUsersDto>> GetUsers([FromBody] TableRequestDto request)
        {
            var result = await _userService.GetUserTableAsync(request);
            return Ok(result);
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<UserDetailInfoDto>> GetUserById(Guid userId)
        {
            var result = await _userService.GetUserDetailAsync(userId);
            return Ok(result);
        }

        [HttpPost("search")]
        public async Task<ActionResult<SearchResponseDto>> SearchItems([FromBody] SearchRequestDto request)
        {
            try
            {
                var result = await _userService.GetSearchResultAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during search" });
            }
        }

        [HttpGet("hierarchy")]
        [ProducesResponseType(typeof(HierarchyResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<HierarchyResponseDto>> GetDepartmentHierarchy()
        {
            try
            {
                var hierarchy = await _userService.GetDepartmentHierarchyAsync();

                if (hierarchy?.Ceo == null && hierarchy?.Departments?.Count == 0)
                {
                    return NotFound("No organizational hierarchy found");
                }

                return Ok(hierarchy);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving organizational hierarchy" });
            }
        }
    }
}
