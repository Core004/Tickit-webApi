using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Infrastructure.Persistence;

namespace TicketSystem.Infrastructure.Identity;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public TokenService(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60")),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token, string userId)
    {
        var revokedToken = await _context.RevokedTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.UserId == userId);

        return revokedToken is null;
    }

    public async Task RevokeTokenAsync(string token, string userId, string? reason = null)
    {
        var revokedToken = new RevokedToken
        {
            Token = token,
            UserId = userId,
            RevokedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(
                int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
            Reason = reason
        };

        _context.RevokedTokens.Add(revokedToken);
        await _context.SaveChangesAsync();
    }
}
