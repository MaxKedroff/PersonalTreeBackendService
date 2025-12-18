using Application.Dtos;
using Application.Interfaces;
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
        //private readonly ILdapService _ldapService;
        private readonly ISynchronizationService _syncService;
        private readonly ILogger<LdapController> _logger;


        public LdapController(
            ISynchronizationService syncService,
            ILogger<LdapController> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }



        [HttpPost("sync-ldap")]
        public async Task<ActionResult<SynchroResponseDto>> SyncLdap([FromBody] SynchroRequestDto request)
        {
            try
            {
                _logger.LogInformation("Запрос на синхронизацию LDAP. Пользователей: {Count}, HardSync: {IsHard}",
                    request.count, request.isHardSynchronize);

                if (request.users == null || request.users.Count == 0)
                {
                    return BadRequest(new SynchroResponseDto
                    {
                        Status = "error",
                        Errors = {"список пользователей пуст"}
                    });
                }

                if (request.count != request.users.Count)
                {
                    _logger.LogWarning("Несоответствие Count: заявлено {Declared}, фактически {Actual}",
                        request.count, request.users.Count);
                    request.count = request.users.Count;
                }

                var result = await _syncService.SyncData(request);

                if (result.Status == "error")
                {
                    return StatusCode(500, result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при синхронизации LDAP");
                return StatusCode(500, new SynchroResponseDto
                {
                    Status = "error",
                    Errors = { $"Внутренняя ошибка сервера: {ex.Message}" }
                });
            }
        }
    }
}
