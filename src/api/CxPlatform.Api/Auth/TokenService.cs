using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CxPlatform.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CxPlatform.Api.Auth;

public class TokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> opts) { _settings = opts.Value; }

    public string IssueAccessToken(User u)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, u.Email),
            new(ClaimTypes.Role, u.Role),
            new("name_en", u.NameEn),
            new("name_ar", u.NameAr),
            new("landing", u.Landing),
        };
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string token, string hash, DateTime expiresAt) IssueRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        var token = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return (token, hash, DateTime.UtcNow.AddDays(_settings.RefreshTokenDays));
    }

    public string HashRefreshToken(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
