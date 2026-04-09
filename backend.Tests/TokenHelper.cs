using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ToroFitDreaming4.Tests;

public static class TokenHelper
{
    public static string GenerateToken(string username, params string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestFactory.TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: TestFactory.TestJwtIssuer,
            audience: TestFactory.TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
