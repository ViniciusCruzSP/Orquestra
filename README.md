# Orquestra — backend local

O Orquestra é um SaaS B2B para governança de ativos de TI. Este incremento contém somente a API ASP.NET Core, a aplicação, o domínio e a persistência SQL Server. O frontend que já existia no repositório foi preservado, mas não faz parte desta entrega nem do build da solução.

## Estrutura

Os quatro projetos existentes foram mantidos na raiz do repositório:

| Projeto | Responsabilidade |
| --- | --- |
| `Orquestra.Domain` | Entidades, estados e regras de transição, sem dependência de EF ou HTTP |
| `Orquestra.Application` | DTOs, validação e casos de uso |
| `Orquestra.Infrastructure` | EF Core, SQL Server, Identity, JWT e migrations |
| `Orquestra.API` | Rotas HTTP, autorização, CORS, Swagger e tratamento de erros |

`Orquestra.slnx` compila esses quatro projetos. As ações de negócio pertencem a uma organização; o identificador dela vem do usuário autenticado, não do corpo das requisições. Consultas por ID e operações de escrita aplicam esse escopo, além dos filtros do EF Core.

## Pré-requisitos

- .NET SDK 10;
- SQL Server local ou SQL Server Express LocalDB (no Windows, instância `MSSQLLocalDB`);
- `dotnet-ef` 10.0.12 para aplicar ou criar migrations manualmente.

Para instalar a ferramenta do EF Core, caso necessário:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
```

## Configuração local

Execute os comandos abaixo na raiz do repositório. Os valores sensíveis ficam em *user-secrets*, fora do Git. Troque o endereço da conexão se usar outra instância local. O exemplo usa autenticação integrada do Windows e não contém credenciais.

```powershell
dotnet user-secrets set --project Orquestra.API "ConnectionStrings:Orquestra" "Server=(localdb)\MSSQLLocalDB;Database=OrquestraDev;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set --project Orquestra.API "Jwt:Issuer" "Orquestra.Local"
dotnet user-secrets set --project Orquestra.API "Jwt:Audience" "Orquestra.Local"
$jwtKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set --project Orquestra.API "Jwt:Key" $jwtKey
```

Para criar a primeira organização e o primeiro administrador no banco vazio, configure também os quatro valores de bootstrap. A senha deve ter ao menos 12 caracteres, incluindo maiúscula, minúscula, número e símbolo. Não a coloque em `appsettings.json` ou em um arquivo versionado.

```powershell
dotnet user-secrets set --project Orquestra.API "Bootstrap:OrganizationName" "Minha Empresa"
dotnet user-secrets set --project Orquestra.API "Bootstrap:OrganizationSlug" "minha-empresa"
dotnet user-secrets set --project Orquestra.API "Bootstrap:AdminEmail" "admin@minha-empresa.example"
dotnet user-secrets set --project Orquestra.API "Bootstrap:AdminPassword" "<senha-forte-escolhida-localmente>"
```

Depois de criar o administrador, remova os quatro valores de bootstrap. A inicialização seguinte reconhecerá a organização existente, sem precisar manter a senha localmente:

```powershell
dotnet user-secrets remove --project Orquestra.API "Bootstrap:AdminPassword"
dotnet user-secrets remove --project Orquestra.API "Bootstrap:AdminEmail"
dotnet user-secrets remove --project Orquestra.API "Bootstrap:OrganizationSlug"
dotnet user-secrets remove --project Orquestra.API "Bootstrap:OrganizationName"
```

Para criar outra organização local, informe um novo nome, slug, e-mail de administrador e senha de bootstrap, então reinicie a API. O bootstrap não troca a senha de contas já existentes.

Para configurar uma origem de navegador permitida, defina `Cors:AllowedOrigins:0` com a URL exata. A lista vazia padrão não habilita nenhuma origem cruzada.

```powershell
dotnet user-secrets set --project Orquestra.API "Cors:AllowedOrigins:0" "http://localhost:5173"
```

O exemplo de CORS apenas configura uma origem local, quando for necessária. Não há frontend novo neste incremento.

## Banco e execução

```powershell
dotnet restore Orquestra.slnx
dotnet build Orquestra.slnx --no-restore
$env:ConnectionStrings__Orquestra = "Server=(localdb)\MSSQLLocalDB;Database=OrquestraDev;Trusted_Connection=True;TrustServerCertificate=True"
dotnet ef database update --project Orquestra.Infrastructure --startup-project Orquestra.Infrastructure
Remove-Item Env:\ConnectionStrings__Orquestra
dotnet run --project Orquestra.API --launch-profile https
```

Em ambiente `Development`, abra `https://localhost:7281/swagger`. Caso o certificado HTTPS local ainda não seja confiável, execute `dotnet dev-certs https --trust`. A API aplica as migrations e inicializa os papéis no início; o comando `dotnet ef database update` permite também verificar o banco explicitamente. Na ferramenta do EF, passe a conexão por `ConnectionStrings__Orquestra` para evitar atualizar outro banco. O bootstrap de organização e administrador é idempotente quando os quatro valores estão presentes.

## Autenticação e regras principais

Use `POST /api/auth/login` com `email` e `password` para obter um JWT; envie-o em `Authorization: Bearer <token>`. `GET /api/auth/me` mostra o usuário autenticado. O token expira em uma hora; não há refresh token neste incremento, então faça novo login quando ele expirar. Cinco falhas de senha bloqueiam o login por 15 minutos. O cadastro inicial de usuário é feito pelo bootstrap local. Nesta versão, o e-mail de login é único no sistema e cada usuário pertence a uma organização.

Os papéis são `Admin`, `GestorTI`, `OperadorTI` e `Auditor`. `Admin`, `GestorTI` e `OperadorTI` podem operar registros. Descarte, inativação de ativo e conclusão de offboarding exigem `Admin` ou `GestorTI`. Somente `Admin` e `Auditor` consultam `/api/audit-logs`.

O bootstrap cria o administrador de cada organização e inicializa os quatro papéis. Ainda não há endpoint para cadastrar outros usuários ou atribuir papéis; essa administração será um incremento separado. Nenhuma rota pública aceita `OrganizationId`.

| Área | Rotas principais |
| --- | --- |
| Cadastros | `/api/departments`, `/api/employees`, `/api/asset-categories`, `/api/assets` |
| Ativo | `/api/assets/{id}/history`, `/maintenance`, `/complete-maintenance`, `/mark-defective`, `/discard` |
| Custódia | `/api/custodies`, `/api/custodies/{id}`, `/api/custodies/{id}/return` |
| Desligamento | `/api/offboardings`, `/api/offboardings/{id}`, `/api/offboardings/{id}/complete` |
| Indicadores e auditoria | `/api/dashboard`, `/api/audit-logs` |

As listagens aceitam `page`, `pageSize` (máximo 100) e `search`; ativos e colaboradores também aceitam filtro de estado. Exclusões dos cadastros inativam registros para preservar a rastreabilidade. A atualização de ativo exige o `rowVersion` devolvido pela leitura; o cliente deve recarregar o registro após conflito de concorrência. O status do ativo só muda pelas operações de entrega, devolução, manutenção, defeito e descarte. Uma entrega agrupa vários ativos disponíveis; a devolução pode definir cada item como disponível, em manutenção ou defeituoso. O desligamento só é concluído depois da devolução de todos os itens pendentes.

Erros de validação, conflito, ausência e autorização usam respostas HTTP consistentes sem stack trace em produção. Movimentações e auditoria registram as ações relevantes sem senhas, tokens ou segredos.
