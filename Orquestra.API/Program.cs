using System.Globalization;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Orquestra.Api;
using Orquestra.Application.Abstractions;
using Orquestra.Application.Services;
using Orquestra.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var portugueseCulture = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = portugueseCulture;
CultureInfo.DefaultThreadCurrentUICulture = portugueseCulture;

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IOrquestraService, OrquestraService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events ??= new JwtBearerEvents();
    options.Events.OnChallenge = async context =>
    {
        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Autenticação necessária"
        }, options: null, contentType: "application/problem+json");
    };
    options.Events.OnForbidden = async context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Acesso negado"
        }, options: null, contentType: "application/problem+json");
    };
});

builder.Services.AddControllers(options =>
    {
        var messages = options.ModelBindingMessageProvider;
        messages.SetMissingBindRequiredValueAccessor(field => $"O campo {field} é obrigatório.");
        messages.SetMissingKeyOrValueAccessor(() => "É necessário informar uma chave ou um valor.");
        messages.SetValueMustNotBeNullAccessor(field => $"O campo {field} não pode ser nulo.");
        messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"O valor '{value}' é inválido para o campo {field}.");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"O valor '{value}' é inválido.");
        messages.SetUnknownValueIsInvalidAccessor(field => $"O valor informado é inválido para o campo {field}.");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "O valor informado é inválido.");
        messages.SetValueIsInvalidAccessor(value => $"O valor '{value}' é inválido.");
        messages.SetValueMustBeANumberAccessor(field => $"O campo {field} deve ser um número.");
        messages.SetNonPropertyValueMustBeANumberAccessor(() => "O valor informado deve ser um número.");
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("recuperacao", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(10) }));
});

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().RequireRole(Roles.Admin, Roles.GestorTI, Roles.OperadorTI, Roles.Auditor).Build())
    .AddPolicy(Policies.Sessao, policy => policy.RequireAuthenticatedUser())
    .AddPolicy(Policies.Colaborador, policy => policy.RequireRole(Roles.Colaborador))
    .AddPolicy(Policies.ManageRecords, policy => policy.RequireRole(Roles.Admin, Roles.GestorTI, Roles.OperadorTI))
    .AddPolicy(Policies.ManageCritical, policy => policy.RequireRole(Roles.Admin, Roles.GestorTI))
    .AddPolicy(Policies.Audit, policy => policy.RequireRole(Roles.Admin, Roles.Auditor));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("ConfiguredOrigins", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    }
}));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API do Orquestra",
        Version = "v1",
        Description = "Serviços para gestão e rastreabilidade de ativos de TI."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});

var app = builder.Build();
await app.Services.InitializeInfrastructureAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("ConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();
