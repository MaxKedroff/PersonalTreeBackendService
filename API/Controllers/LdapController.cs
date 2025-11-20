using Application.Dtos;
using Infrastructure.ActiveDirectory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Контроллер для интеграции с Active Directory через LDAP.
    /// Предоставляет доступ к иерархии и данным пользователей из домена.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LdapController : ControllerBase
    {
        private readonly ILdapService _ldapService;

        public LdapController(ILdapService ldapService)
        {
            _ldapService = ldapService;
        }

        /// <summary>
        /// Получает иерархию организационных подразделений и пользователей из Active Directory.
        /// </summary>
        /// <returns>Древовидная структура подразделений и сотрудников.</returns>
        /// <response code="200">Иерархия успешно получена.</response>
        /// <response code="500">Ошибка при подключении к LDAP.</response>
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

        /// <summary>
        /// Получает список всех пользователей из Active Directory.
        /// </summary>
        /// <returns>Список пользователей с основной информацией.</returns>
        /// <response code="200">Список пользователей успешно получен.</response>
        /// <response code="500">Ошибка при получении данных из LDAP.</response>
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

        /// <summary>
        /// Получает информацию о пользователе по его SAM-имени (логину в домене).
        /// </summary>
        /// <param name="samAccountName">SAM-аккаунт пользователя (например, "jdoe").</param>
        /// <returns>Информация о пользователе или 404, если не найден.</returns>
        /// <response code="200">Пользователь найден.</response>
        /// <response code="404">Пользователь не найден.</response>
        /// <response code="500">Ошибка при обращении к LDAP.</response>
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
