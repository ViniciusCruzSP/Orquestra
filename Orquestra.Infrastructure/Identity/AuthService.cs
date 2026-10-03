using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Orquestra.Application.Abstractions;
using Orquestra.Application.Auth;

namespace Orquestra.Infrastructure.Identity;

internal sealed class AuthService(
    UserManager<ApplicationUser> users,
    ICurrentUser currentUser,
    JwtOptions jwt) : IAuthService
{
    public async Task<AuthResult?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByEmailAsync(email.Trim());
        if (user is null || await users.IsLockedOutAsync(user)) return null;

        if (!await users.CheckPasswordAsync(user, password))
        {
            await users.AccessFailedAsync(user);
            return null;
        }

        await users.ResetAccessFailedCountAsync(user);
        var roles = await users.GetRolesAsync(user);
        if (roles.Count == 0) return null;

        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("org_id", user.OrganizationId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                SecurityAlgorithms.HmacSha256));

        return new AuthResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt,
            user.Id, user.Email!, DisplayRole(roles[0]), user.OrganizationId);
    }

    public async Task<CurrentUserResult?> GetMeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null || currentUser.OrganizationId == Guid.Empty ||
            user.OrganizationId != currentUser.OrganizationId) return null;

        var roles = await users.GetRolesAsync(user);
        if (roles.Count == 0) return null;
        return new CurrentUserResult(user.Id, user.Email!, DisplayRole(roles[0]), user.OrganizationId);
    }

    private static string DisplayRole(string role) => role switch
    {
        "Admin" => "Administrador",
        "GestorTI" => "Gestor de TI",
        "OperadorTI" => "Operador de TI",
        "Auditor" => "Auditor",
        _ => role
    };
}
