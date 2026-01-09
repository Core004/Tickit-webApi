using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    Task<bool> ValidateRefreshTokenAsync(string token, string userId);
    Task RevokeTokenAsync(string token, string userId, string? reason = null);
}
