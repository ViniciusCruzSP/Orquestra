using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Orquestra.Application.Abstractions;
using Orquestra.Application.Auth;
using Orquestra.Infrastructure.Identity;
using Orquestra.Infrastructure.Persistence;

namespace Orquestra.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Orquestra");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Orquestra deve ser configurada fora do controle de versão.");

        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var key = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:Issuer, Jwt:Audience e uma Jwt:Key com pelo menos 32 bytes são obrigatórios.");

        var jwt = new JwtOptions(issuer, audience, key);
        services.AddSingleton(jwt);
        services.AddDbContext<OrquestraDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IOrquestraDb>(provider => provider.GetRequiredService<OrquestraDbContext>());
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IGeradorTermo, GeradorTermoPdf>();
        services.AddScoped<DatabaseBootstrapper>();
        services.AddDataProtection();

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddErrorDescriber<PortugueseIdentityErrorDescriber>()
        .AddRoles<IdentityRole>()
        .AddDefaultTokenProviders()
        .AddEntityFrameworkStores<OrquestraDbContext>();

        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromMinutes(30));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.Events = new JwtBearerEvents { OnTokenValidated = ContaValida.ValidarToken };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        return services;
    }

    public static async Task InitializeInfrastructureAsync(this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>()
            .InitializeAsync(cancellationToken);
    }
}
