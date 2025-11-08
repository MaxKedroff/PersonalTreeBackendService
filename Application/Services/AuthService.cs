using Application.Dtos;
using Application.Interfaces;
using Core.Utils;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<string> AuthenticateAsync(LoginDTO login)
        {
            _logger.LogInformation("Authentication attempt for user: {Login}", login.Username);
            if (string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
            {
                _logger.LogWarning("Empty login or password provided");
                throw new UnauthorizedAccessException("Login and password are required");
            }
            var user = await _userRepository.GetUserByLoginAsync(login.Username);
            if (user == null)
            {
                _logger.LogWarning("User not found: {Login}", login.Username);
                throw new UnauthorizedAccessException("Invalid login or password");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Inactive user attempted login: {Login}", login.Username);
                throw new UnauthorizedAccessException("Account is inactive");
            }
            var token = GenerateJwtToken(user);
            _logger.LogInformation("User authenticated successfully: {Login}, Role: {Role}",
                login.Username, user.Role);
            return token;
        }
        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.User_id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.GetFullName() ?? user.Login),
                new Claim("Email", user.Email ?? string.Empty)
            };

            if (user.IsAdmin())
            {
                claims.Add(new Claim("IsAdmin", "true"));
            }
            if (user.IsHr())
            {
                claims.Add(new Claim("IsHr", "true"));
            }

            var credentials = new SigningCredentials(
                AuthOptions.GetSymmetricSecurityKey(),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24), 
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<User> RegisterUserAsync(CreateUserDTO register)
        {
            throw new NotImplementedException();
        }
    }
}
