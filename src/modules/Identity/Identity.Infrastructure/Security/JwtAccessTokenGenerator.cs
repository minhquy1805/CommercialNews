using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommercialNews.BuildingBlocks.SharedKernel.Time;
using Identity.Application.Ports.Services;
using Identity.Application.Ports.Services.Models;
using Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Security;

public sealed class JwtAccessTokenGenerator : IAccessTokenGenerator
{
    private readonly JwtSettings _jwtSettings;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtAccessTokenGenerator(
        IOptions<JwtSettings> jwtSettings,
        IDateTimeProvider dateTimeProvider)
    {
        _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));

        if (string.IsNullOrWhiteSpace(_jwtSettings.Issuer))
        {
            throw new ArgumentException("JWT issuer is required.", nameof(jwtSettings));
        }

        if (string.IsNullOrWhiteSpace(_jwtSettings.Audience))
        {
            throw new ArgumentException("JWT audience is required.", nameof(jwtSettings));
        }

        if (string.IsNullOrWhiteSpace(_jwtSettings.SecretKey))
        {
            throw new ArgumentException("JWT secret key is required.", nameof(jwtSettings));
        }

        if (_jwtSettings.SecretKey.Length < 32)
        {
            throw new ArgumentException("JWT secret key must be at least 32 characters long.", nameof(jwtSettings));
        }

        if (_jwtSettings.AccessTokenLifetimeMinutes <= 0)
        {
            throw new ArgumentException("JWT access token lifetime must be greater than zero.", nameof(jwtSettings));
        }
    }

    public AccessTokenResult Generate(UserAccount userAccount)
    {
        ArgumentNullException.ThrowIfNull(userAccount);

        DateTime nowUtc = _dateTimeProvider.UtcNow;
        DateTime expiresAtUtc = nowUtc.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userAccount.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, userAccount.Email),
            new("public_id", userAccount.PublicId),
            new("email_verified", userAccount.IsEmailVerified.ToString().ToLowerInvariant()),
            new("status", userAccount.Status)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResult
        {
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}