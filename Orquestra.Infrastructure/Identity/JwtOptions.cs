namespace Orquestra.Infrastructure.Identity;

internal sealed record JwtOptions(string Issuer, string Audience, string Key);
