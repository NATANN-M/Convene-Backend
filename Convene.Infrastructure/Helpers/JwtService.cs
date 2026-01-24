using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Convene.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Convene.Infrastructure.Helpers
{
    public class JwtService
    {
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;

        public JwtService(IConfiguration configuration)
        {
            _secret = configuration["Jwt:Secret"] ?? throw new ArgumentNullException("Jwt:Secret not found in configuration");
            _issuer = configuration["Jwt:Issuer"] ?? "ConveneAPI";
            _audience = configuration["Jwt:Audience"] ?? "ConveneClient";
        }

        public string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
 {
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
    new Claim(ClaimTypes.Role, user.Role.ToString())
};


            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
