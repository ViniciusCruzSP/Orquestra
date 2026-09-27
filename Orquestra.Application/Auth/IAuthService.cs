namespace Orquestra.Application.Auth;

public record AuthResult(string Token, DateTimeOffset ExpiresAt, string UserId, string Email, string Role, Guid OrganizationId);
public record CurrentUserResult(string UserId, string Email, string Role, Guid OrganizationId);

public interface IAuthService
{
    Task<AuthResult?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<CurrentUserResult?> GetMeAsync(string userId, CancellationToken cancellationToken = default);
}
