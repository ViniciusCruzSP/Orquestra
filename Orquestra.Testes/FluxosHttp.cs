using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static Orquestra.Testes.ApiDeTeste;
using static System.Net.HttpStatusCode;

namespace Orquestra.Testes;

public sealed class FluxosHttp : IClassFixture<ApiDeTeste>
{
    private readonly ApiDeTeste api;

    public FluxosHttp(ApiDeTeste api) => this.api = api;

    [TesteSql]
    public async Task ColaboradorSemContaPodeSerEditadoEReceberAcessoDepois()
    {
        using var admin = await api.Entrar();
        using var visitante = api.NovoCliente();
        var email = EmailNovo();
        var id = await CriarColaborador(admin, email);
        var rota = $"api/colaboradores/{id}";
        var conta = $"api/contas/{id}";
        await Conferir(await visitante.GetAsync(rota), Unauthorized);
        await Conferir(await admin.GetAsync(conta), NoContent);
        await Conferir(await admin.GetAsync($"api/contas/{Guid.NewGuid()}"), NotFound);
        await Conferir(await visitante.PostAsJsonAsync("api/autenticacao/entrar", new { email, senha = Senha }), Unauthorized);
        var editado = await Json(await admin.PutAsJsonAsync(rota, new { nomeCompleto = "Nome atualizado", email }), OK);
        Assert.Equal("Nome atualizado", editado.GetProperty("nomeCompleto").GetString());
        await Conferir(await admin.PutAsJsonAsync(conta, new { perfil = "Colaborador" }), BadRequest);
        await Conferir(await admin.PutAsJsonAsync(conta, new { perfil = "Colaborador", senhaInicial = "fraca" }), BadRequest);
        await Conferir(await admin.GetAsync(conta), NoContent);
        var acesso = await Json(await admin.PutAsJsonAsync(conta, new { perfil = "Colaborador", senhaInicial = Senha }), OK);
        Assert.Equal(email, acesso.GetProperty("email").GetString());
        using var colaborador = await api.Entrar(email);
        var semAtivos = await Json(await colaborador.GetAsync("api/minha-area/custodias"), OK);
        Assert.Equal(0, semAtivos.GetArrayLength());
        await Conferir(await colaborador.GetAsync("api/colaboradores"), Forbidden);
        await Conferir(await admin.PutAsJsonAsync(conta, new { perfil = "Auditor" }), OK);
        await Conferir(await colaborador.GetAsync("api/autenticacao/usuario-atual"), Unauthorized);
        using var auditor = await api.Entrar(email);
        await Conferir(await auditor.GetAsync("api/auditoria"), OK);
        await Conferir(await auditor.PutAsJsonAsync(rota, new { nomeCompleto = "Alteração indevida", email }), Forbidden);
        await Conferir(await admin.DeleteAsync(rota), NoContent);
        await Conferir(await auditor.GetAsync("api/autenticacao/usuario-atual"), Unauthorized);
    }

    [TesteSql]
    public async Task DuplicidadeValidacaoEPaginacaoDeColaboradores()
    {
        using var admin = await api.Entrar();
        var email = EmailNovo();
        await CriarColaborador(admin, email);
        await Conferir(await admin.PostAsJsonAsync("api/colaboradores", new { nomeCompleto = "Outro", email }), Conflict);
        await Conferir(await admin.PostAsJsonAsync("api/colaboradores", new { nomeCompleto = "Outro", email = "inválido" }), BadRequest);
        await Conferir(await admin.GetAsync("api/colaboradores?pagina=0"), BadRequest);
        await Conferir(await admin.GetAsync("api/colaboradores?situacao=inexistente"), BadRequest);
        var lista = await Json(await admin.GetAsync($"api/colaboradores?busca={Uri.EscapeDataString(email)}&situacao=Ativo"), OK);
        Assert.Equal(1, lista.GetProperty("total").GetInt32());
    }

    [TesteSql]
    public async Task DepartamentoComColaboradorECategoriaComAtivoNaoPodemSerInativados()
    {
        using var admin = await api.Entrar();
        var nome = $"TI {Guid.NewGuid():N}";
        var departamento = Id(await Json(await admin.PostAsJsonAsync("api/departamentos", new { nome }), Created));
        await Conferir(await admin.PostAsJsonAsync("api/departamentos", new { nome }), Conflict);
        var pessoa = Id(await Json(await admin.PostAsJsonAsync("api/colaboradores",
            new { nomeCompleto = "Pessoa de teste", email = EmailNovo(), departamentoId = departamento }), Created));
        await Conferir(await admin.DeleteAsync($"api/departamentos/{departamento}"), Conflict);
        await Conferir(await admin.DeleteAsync($"api/colaboradores/{pessoa}"), NoContent);
        await Conferir(await admin.DeleteAsync($"api/departamentos/{departamento}"), NoContent);
        await Conferir(await admin.PostAsJsonAsync("api/colaboradores",
            new { nomeCompleto = "Pessoa de teste", email = EmailNovo(), departamentoId = departamento }), BadRequest);

        var id = await CriarAtivo(admin);
        var ativo = await Json(await admin.GetAsync($"api/ativos/{id}"), OK);
        var categoria = ativo.GetProperty("categoriaId").GetGuid();
        await Conferir(await admin.DeleteAsync($"api/categorias-de-ativos/{categoria}"), Conflict);
        await Conferir(await admin.DeleteAsync($"api/ativos/{id}"), NoContent);
        await Conferir(await admin.DeleteAsync($"api/categorias-de-ativos/{categoria}"), NoContent);
    }

    [TesteSql]
    public async Task EntregaComDoisAtivosGeraTermoEExigeDevolucaoParaDesligar()
    {
        using var admin = await api.Entrar();
        var colaboradorId = await CriarColaborador(admin, EmailNovo());
        var principal = await CriarAtivo(admin);
        var secundario = await CriarAtivo(admin);
        var rota = "api/custodias";
        await Conferir(await admin.PostAsJsonAsync(rota, new { colaboradorId, ativosIds = Array.Empty<Guid>() }), BadRequest);
        await Conferir(await admin.PostAsJsonAsync(rota, new { colaboradorId, ativosIds = new[] { principal, principal } }), BadRequest);
        var entrega = Id(await Json(await admin.PostAsJsonAsync(rota, new { colaboradorId, ativosIds = new[] { principal, secundario } }), Created));
        await Conferir(await admin.PostAsJsonAsync(rota, new { colaboradorId, ativosIds = new[] { principal } }), Conflict);
        await Conferir(await admin.DeleteAsync($"api/colaboradores/{colaboradorId}"), Conflict);
        var termo = await BaixarPdf(admin, entrega);
        var outroEmail = EmailNovo();
        var outraPessoa = await CriarColaborador(admin, outroEmail);
        var terceiro = await CriarAtivo(admin);
        var outraEntrega = Id(await Json(await admin.PostAsJsonAsync(rota,
            new { colaboradorId = outraPessoa, ativosIds = new[] { terceiro } }), Created));
        await Conferir(await admin.PutAsJsonAsync($"api/contas/{outraPessoa}",
            new { perfil = "Colaborador", senhaInicial = Senha }), OK);
        using var colaborador = await api.Entrar(outroEmail);
        var listaPessoal = await Json(await colaborador.GetAsync("api/minha-area/custodias"), OK);
        var pessoal = Assert.Single(listaPessoal.EnumerateArray());
        Assert.Equal(outraEntrega, Id(pessoal));
        Assert.Equal(terceiro, Assert.Single(pessoal.GetProperty("itens").EnumerateArray()).GetProperty("ativoId").GetGuid());
        await Conferir(await colaborador.GetAsync($"api/minha-area/custodias/{outraEntrega}/termo"), OK);

        var desligamento = Id(await Json(await admin.PostAsJsonAsync("api/desligamentos", new { colaboradorId }), Created));
        var concluir = $"api/desligamentos/{desligamento}/concluir";
        await Conferir(await admin.PostAsync(concluir, null), Conflict);
        await Conferir(await admin.PostAsJsonAsync($"api/custodias/{entrega}/devolver", new
        {
            itens = new[] { new { ativoId = principal, destino = "Disponível" },
                new { ativoId = Guid.NewGuid(), destino = "Disponível" } }
        }), Conflict);
        await Conferir(await Devolver(admin, entrega, Guid.NewGuid(), "Disponível"), Conflict);
        await Conferir(await Devolver(admin, entrega, principal, "ComDefeito"), OK);
        await Conferir(await Devolver(admin, entrega, principal, "ComDefeito"), Conflict);
        await Conferir(await admin.PostAsync(concluir, null), Conflict);
        await Conferir(await Devolver(admin, entrega, secundario, "Disponível"), OK);
        await Conferir(await admin.PostAsync(concluir, null), OK);
        await Conferir(await admin.PostAsync(concluir, null), Conflict);
        Assert.Equal(termo, await BaixarPdf(admin, entrega));
        var ativo = await Json(await admin.GetAsync($"api/ativos/{principal}"), OK);
        Assert.Equal("ComDefeito", ativo.GetProperty("situacao").GetString());
    }

    [TesteSql]
    public async Task InventarioRejeitaAlteracaoAntigaEAuditaMovimentacao()
    {
        using var admin = await api.Entrar();
        var id = await CriarAtivo(admin);
        var rota = $"api/ativos/{id}";
        var original = await Json(await admin.GetAsync(rota), OK);
        var versao = original.GetProperty("versaoDoRegistro").GetString();
        await Conferir(await admin.PostAsJsonAsync($"{rota}/concluir-manutencao", new { observacoes = "" }), Conflict);
        await Conferir(await admin.PostAsJsonAsync($"{rota}/manutencao", new { observacoes = "Revisão" }), OK);
        await Conferir(await admin.PutAsJsonAsync(rota, new
        {
            categoriaId = original.GetProperty("categoriaId").GetGuid(),
            patrimonio = original.GetProperty("patrimonio").GetString(), nome = "Antigo", marca = "Marca", modelo = "Modelo",
            versaoDoRegistro = versao
        }), Conflict);
        var historico = await Json(await admin.GetAsync($"{rota}/historico"), OK);
        Assert.Contains(historico.GetProperty("itens").EnumerateArray(), item =>
            item.GetProperty("tipo").GetString() == "ManutençãoIniciada");
        var patrimonio = original.GetProperty("patrimonio").GetString();
        var relatorio = await Json(await admin.GetAsync($"api/relatorios/historico?busca={patrimonio}&operacao=4"), OK);
        Assert.Equal(id, Assert.Single(relatorio.GetProperty("itens").EnumerateArray()).GetProperty("ativoId").GetGuid());
    }

    [TesteSql]
    public async Task RecuperacaoSemSmtpERedefinicaoComCodigoInvalido()
    {
        using var cliente = api.NovoCliente();
        var email = Admin;
        await Conferir(await cliente.PostAsJsonAsync("api/autenticacao/recuperar-senha", new { email }), ServiceUnavailable);
        await Conferir(await cliente.PostAsJsonAsync("api/autenticacao/redefinir-senha",
            new { email, codigo = "inválido", senha = Senha }), BadRequest);
    }

    private static string EmailNovo() => $"{Guid.NewGuid():N}@orquestra.test";
    private static Guid Id(JsonElement item) => item.GetProperty("id").GetGuid();

    private static async Task<Guid> CriarColaborador(HttpClient cliente, string email) =>
        Id(await Json(await cliente.PostAsJsonAsync("api/colaboradores", new { nomeCompleto = "Pessoa de teste", email }), Created));

    private static async Task<Guid> CriarAtivo(HttpClient cliente)
    {
        var categoriaId = Id(await Json(await cliente.PostAsJsonAsync("api/categorias-de-ativos",
            new { nome = $"Categoria {Guid.NewGuid():N}" }), Created));
        return Id(await Json(await cliente.PostAsJsonAsync("api/ativos", new
        {
            categoriaId, nome = "Notebook de teste", patrimonio = Guid.NewGuid().ToString("N"),
            marca = "Marca", modelo = "Modelo"
        }), Created));
    }

    private static Task<HttpResponseMessage> Devolver(HttpClient cliente, Guid entrega, Guid ativoId, string destino) =>
        cliente.PostAsJsonAsync($"api/custodias/{entrega}/devolver", new { itens = new[] { new { ativoId, destino } } });

    private static async Task<byte[]> BaixarPdf(HttpClient cliente, Guid entrega)
    {
        var resposta = await cliente.GetAsync($"api/custodias/{entrega}/termo");
        await Conferir(resposta, OK);
        Assert.Equal("application/pdf", resposta.Content.Headers.ContentType?.MediaType);
        var pdf = await resposta.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
        return pdf;
    }
}
