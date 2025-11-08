using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Core.Utils
{
    public class AuthOptions
    {
        public const string ISSUER = "MyAuthServer"; 
        public const string AUDIENCE = "MyAuthClient";
        const string KEY = "mysupersecret_secretsecretsecretkey!123";   
        public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));

        public const string ROLE_USER = "User";
        public const string ROLE_HR = "Hr";
        public const string ROLE_ADMIN = "Admin";

        public const string POLICY_USER = "UserPolicy";
        public const string POLICY_HR = "HrPolicy";
        public const string POLICY_ADMIN = "AdminPolicy";

        public static string GenerateJwtToken(jwtDTO credentials)
        {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, credentials.userID.ToString()),

                new Claim(ClaimTypes.Name, credentials.username) };


            var token = new JwtSecurityToken(
                issuer: ISSUER,
                audience: AUDIENCE,
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: new SigningCredentials(GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));


            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static ClaimsPrincipal ValidateAndGetPrincipal(string jwt, bool validateLifetime = true)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidIssuer = ISSUER,
                ValidAudience = AUDIENCE,
                IssuerSigningKey = GetSymmetricSecurityKey(),

                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.Zero
            };
            return tokenHandler.ValidateToken(jwt, parameters, out _);
        }

        public static bool TryReadUserInfo(string? authorizationHeader, out int userId)
        {
            userId = default;
            if (string.IsNullOrWhiteSpace(authorizationHeader) ||
                        !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return false;

            var jwt = authorizationHeader["Bearer ".Length..].Trim();
            try
            {
                var principal = ValidateAndGetPrincipal(jwt, validateLifetime: false);
                var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var nameClaim = principal.FindFirst(ClaimTypes.Name)?.Value;

                if (!int.TryParse(idClaim, out userId))
                    return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public class jwtDTO
        {
            public string username;
            public int userID;
        }
    }
}
