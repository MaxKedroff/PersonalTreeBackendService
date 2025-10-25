using Application.Dtos;
using Infrastructure.ActiveDirectory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LdapController : ControllerBase
    {
        private readonly ILdapService _ldapService;

        public LdapController(ILdapService ldapService)
        {
            _ldapService = ldapService;
        }

        [HttpGet("hierarchy")]
        public async Task<ActionResult<LdapHierarchyResponse>> GetLdapHierarchy()
        {
            try
            {
                var hierarchy = await _ldapService.GetLdapHierarchyAsync();
                return Ok(hierarchy);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving LDAP hierarchy");
            }
        }

        [HttpGet("users")]
        public async Task<ActionResult> GetLdapUsers()
        {
            try
            {
                var users = await _ldapService.GetUsersFromActiveDirectoryAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving LDAP users");
            }
        }

        [HttpGet("user/{samAccountName}")]
        public async Task<ActionResult> GetUserBySamAccountName(string samAccountName)
        {
            try
            {
                var user = await _ldapService.GetUserBySamAccountNameAsync(samAccountName);
                if (user == null)
                    return NotFound($"User with SAM account name '{samAccountName}' not found");

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving user");
            }
        }

    }
}
