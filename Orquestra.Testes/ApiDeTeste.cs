using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orquestra.Infrastructure.Persistence;

namespace Orquestra.Testes;

public sealed class TesteSqlAttribute : FactAttribute
{
    public TesteSqlAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORQUESTRA_TESTES_SQL")))
            Skip = "Defina ORQUESTRA_TESTES_SQL para executar os testes de integração.";
    }
}

public sealed class ApiDeTeste : WebApplicationFactory<Program>
{
    public const string Admin = "admin@orquestra.test";
    public const string Senha = "Orquestra.Testes-123!";
    private readonly string conexao = new SqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable("ORQUESTRA_TESTES_SQL"))
    {
        AttachDBFilename = "",
        InitialCatalog = $"OrquestraTeste_{Guid.NewGuid():N}"
    }.ConnectionString;
    private bool iniciado;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Orquestra"] = conexao,
                ["Jwt:Issuer"] = "Orquestra.Testes",
                ["Jwt:Audience"] = "Orquestra.Testes",
                ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
                ["Bootstrap:OrganizationName"] = "Orquestra Testes",
                ["Bootstrap:OrganizationSlug"] = "orquestra-testes",
                ["Bootstrap:AdminEmail"] = Admin,
                ["Bootstrap:AdminPassword"] = Senha,
                ["Email:Servidor"] = null
            }));
        var host = base.CreateHost(builder);
        iniciado = true;
        return host;
    }

    public HttpClient NovoCliente() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    public async Task<HttpClient> Entrar(string email = Admin, string senha = Senha)
    {
        var cliente = NovoCliente();
        var login = await Json(await cliente.PostAsJsonAsync("api/autenticacao/entrar", new { email, senha }), HttpStatusCode.OK);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("token").GetString());
        return cliente;
    }

    public static async Task Conferir(HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        Assert.True(resposta.StatusCode == esperado,
            $"Esperado {esperado}; recebido {resposta.StatusCode} em {resposta.RequestMessage?.RequestUri?.AbsolutePath}. " +
            (resposta.IsSuccessStatusCode ? "" : await resposta.Content.ReadAsStringAsync()));
    }

    public static async Task<JsonElement> Json(HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        await Conferir(resposta, esperado);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            if (iniciado)
            {
                await using var escopo = Services.CreateAsyncScope();
                await escopo.ServiceProvider.GetRequiredService<OrquestraDbContext>().Database.EnsureDeletedAsync();
            }
        }
        finally
        {
            await base.DisposeAsync();
        }
    }
}
