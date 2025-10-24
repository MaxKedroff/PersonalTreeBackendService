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
        public async Task<ActionResult<ResponseUsersTreeDto>> GetUsers()
        {
            var result = await _userService.GetUsersAsync();
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
    }
}
