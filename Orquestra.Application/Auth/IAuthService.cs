using System.Text.Json.Serialization;

namespace Orquestra.Application.Auth;

public record AuthResult(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("expiraEm")] DateTimeOffset ExpiresAt,
    [property: JsonPropertyName("usuarioId")] string UserId,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("perfil")] string Role,
    [property: JsonPropertyName("organizacaoId")] Guid OrganizationId);
public record CurrentUserResult(
    [property: JsonPropertyName("usuarioId")] string UserId,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("perfil")] string Role,
    [property: JsonPropertyName("organizacaoId")] Guid OrganizationId);

public interface IAuthService
{
    Task<AuthResult?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<CurrentUserResult?> GetMeAsync(string userId, CancellationToken cancellationToken = default);
}
