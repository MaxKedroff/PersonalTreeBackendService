using Application.Dtos;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>
    /// Контроллер аутентификации. Обрабатывает вход пользователей и выдачу JWT-токенов.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }


        /// <summary>
        /// Аутентифицирует пользователя по логину и паролю и возвращает JWT-токен при успешной проверке.
        /// </summary>
        /// <param name="login">DTO с данными для входа: <see cref="LoginDTO"/> (логин и пароль).</param>
        /// <returns>
        /// Возвращает <see cref="IActionResult"/> с объектом, содержащим JWT-токен, если аутентификация успешна.
        /// В случае ошибки — соответствующий HTTP-статус: 401 (неавторизован) или 500 (внутренняя ошибка).
        /// </returns>
        /// <response code="200">Успешная аутентификация. Возвращает JWT-токен.</response>
        /// <response code="401">Неверные учётные данные или доступ запрещён.</response>
        /// <response code="500">Внутренняя ошибка сервера при попытке аутентификации.</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO login)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {Login}", login.Username);

                var token = await _authService.AuthenticateAsync(login);

                _logger.LogInformation("User {Login} logged in successfully", login.Username);

                return Ok(new { Token = token });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Failed login attempt for user: {Login}, Reason: {Reason}",
                    login.Username, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Login}", login.Username);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }
    }
}
