# Orquestra

O Orquestra nasceu de um problema comum nas empresas: depois de algum tempo, ninguém sabe com certeza onde estão todos os notebooks, monitores e celulares, quem ficou responsável por cada item ou por quais manutenções aquele equipamento já passou.

A proposta do projeto é reunir esse controle em um só lugar. O sistema registra o inventário de ativos de TI, as entregas e devoluções, as manutenções, os defeitos e os descartes. Também acompanha o desligamento de colaboradores para evitar que um processo seja encerrado enquanto ainda existirem equipamentos pendentes.

Este é o repositório da API em ASP.NET Core. A interface em Next.js usa as rotas em português e fica no [repositório Orquestra Web](https://github.com/ViniciusCruzSP/orquestra-web). A pasta `orquestra.web` contém a página Vite original.

## O que já funciona

A API já possui autenticação com JWT, separação dos dados por organização e controle de acesso por perfil. É possível cadastrar departamentos, colaboradores, categorias e ativos; entregar vários ativos em uma única custódia; registrar devoluções; controlar manutenção, defeito e descarte; acompanhar desligamentos; consultar indicadores e revisar o histórico de auditoria.

O frontend em Next.js consome a API para autenticação, cadastros, inventário, custódias, desligamentos e relatórios. Rode os dois projetos em terminais separados.

## Organização do código

| Projeto | O que fica nele |
| --- | --- |
| `orquestra.web` | Interface em React, TypeScript e Vite |
| `Orquestra.API` | Endpoints, autenticação, autorização, Swagger e tratamento de erros |
| `Orquestra.Application` | Casos de uso, validações e contratos da aplicação |
| `Orquestra.Domain` | Entidades, estados e regras de negócio |
| `Orquestra.Infrastructure` | SQL Server, Entity Framework Core, Identity e geração do JWT |

A separação ajuda a manter as regras do Orquestra independentes da interface e do banco de dados. Cada usuário pertence a uma organização, e as consultas usam essa informação para impedir o acesso aos dados de outra empresa.

## Antes de começar

Para executar o projeto localmente, você precisa ter:

- .NET SDK 10;
- SQL Server LocalDB, SQL Server Express ou outra instância compatível;
- `dotnet-ef` 10.0.12, caso queira aplicar as migrations manualmente.

Se a ferramenta do Entity Framework ainda não estiver instalada:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
```

## Configuração da API

Os dados sensíveis ficam no *User Secrets* e não são enviados ao Git. Na raiz do repositório, configure a conexão com o banco e os dados usados para assinar o token:

```powershell
dotnet user-secrets set --project Orquestra.API "ConnectionStrings:Orquestra" "Server=(localdb)\MSSQLLocalDB;Database=OrquestraDev;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set --project Orquestra.API "Jwt:Issuer" "Orquestra.Local"
dotnet user-secrets set --project Orquestra.API "Jwt:Audience" "Orquestra.Local"
$bytes = New-Object byte[] 48
$gerador = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $gerador.GetBytes($bytes) } finally { $gerador.Dispose() }
$chaveJwt = [Convert]::ToBase64String($bytes)
dotnet user-secrets set --project Orquestra.API "Jwt:Key" $chaveJwt
```

Em um banco vazio, o Orquestra precisa criar a primeira organização e o primeiro administrador. Escolha seus próprios dados e use uma senha com pelo menos 12 caracteres, incluindo letra maiúscula, letra minúscula, número e símbolo:

```powershell
dotnet user-secrets set --project Orquestra.API "Bootstrap:OrganizationName" "Minha Empresa"
dotnet user-secrets set --project Orquestra.API "Bootstrap:OrganizationSlug" "minha-empresa"
dotnet user-secrets set --project Orquestra.API "Bootstrap:AdminEmail" "admin@minha-empresa.com.br"
dotnet user-secrets set --project Orquestra.API "Bootstrap:AdminPassword" "<sua-senha-local>"
```

Depois do primeiro acesso, a senha de bootstrap não precisa continuar salva:

```powershell
dotnet user-secrets remove --project Orquestra.API "Bootstrap:AdminPassword"
dotnet user-secrets remove --project Orquestra.API "Bootstrap:AdminEmail"
dotnet user-secrets remove --project Orquestra.API "Bootstrap:OrganizationSlug"
dotnet user-secrets remove --project Orquestra.API "Bootstrap:OrganizationName"
```

## Executando o projeto

Restaure as dependências, prepare o banco e inicie a API:

```powershell
dotnet restore Orquestra.slnx
dotnet build Orquestra.slnx --no-restore
$env:ConnectionStrings__Orquestra = "Server=(localdb)\MSSQLLocalDB;Database=OrquestraDev;Trusted_Connection=True;TrustServerCertificate=True"
dotnet ef database update --project Orquestra.Infrastructure --startup-project Orquestra.Infrastructure
Remove-Item Env:\ConnectionStrings__Orquestra
dotnet run --project Orquestra.API --launch-profile https
```

O Swagger ficará disponível em `https://localhost:7281/swagger`.

Para usar a interface, siga o README do repositório Next.js. O servidor Next.js se comunica com a API sem depender de CORS. Em desenvolvimento, o perfil `http` usa apenas a porta local 5243. Em homologação e produção configure HTTPS.

Se o navegador não confiar no certificado local da API, execute uma vez:

```powershell
dotnet dev-certs https --trust
```

## Testando a autenticação

No Swagger, abra `POST /api/autenticacao/entrar` e envie o e-mail e a senha do administrador:

```json
{
  "email": "admin@minha-empresa.com.br",
  "senha": "<sua-senha-local>"
}
```

Copie somente o valor de `token`, clique em **Authorize** e cole o token no campo apresentado. O Swagger acrescenta o tipo `Bearer` automaticamente. Depois disso, `GET /api/autenticacao/usuario-atual` mostra os dados da sessão autenticada.

O token vale por uma hora. Após cinco tentativas de senha incorreta, o acesso fica bloqueado por 15 minutos.

## Principais rotas

| Área | Rotas |
| --- | --- |
| Autenticação | `/api/autenticacao/entrar`, `/api/autenticacao/usuario-atual` |
| Cadastros | `/api/departamentos`, `/api/colaboradores`, `/api/categorias-de-ativos` |
| Ativos | `/api/ativos`, `/api/ativos/{id}/historico`, `/api/ativos/{id}/manutencao`, `/api/ativos/{id}/concluir-manutencao`, `/api/ativos/{id}/marcar-com-defeito`, `/api/ativos/{id}/descartar` |
| Custódias | `/api/custodias`, `/api/custodias/{id}/devolver` |
| Desligamentos | `/api/desligamentos`, `/api/desligamentos/{id}/concluir` |
| Acompanhamento | `/api/painel`, `/api/auditoria` |

As listagens aceitam `pagina`, `itensPorPagina`, `busca` e, quando aplicável, `situacao`. O limite é de 100 itens por página.

## Regras que protegem o histórico

O Orquestra não apaga fisicamente os principais registros do negócio. Departamentos, categorias, colaboradores e ativos são inativados para que as movimentações anteriores continuem fazendo sentido.

Um ativo só pode estar em uma custódia por vez. A devolução define se ele volta a ficar disponível, segue para manutenção ou é marcado com defeito. Cadastro, edição e inativação de ativos e colaboradores, entrega, devolução e descarte exigem o perfil de Administrador. O descarte exige motivo. O Administrador ou Gestor de TI pode concluir um desligamento depois da devolução de todos os ativos do colaborador.

Ao registrar uma entrega, a API gera um termo de compromisso em PDF para os equipamentos daquela custódia. O PDF guarda os dados da entrega no momento da emissão e contém espaço para assinatura manual. Se a geração falhar, a entrega não é gravada. O administrador usa `GET /api/custodias/{id}/termo`. O colaborador vinculado usa `GET /api/minha-area/custodias/{id}/termo`. Custódias anteriores à funcionalidade podem não ter termo.

O PDF usa [QuestPDF](https://www.questpdf.com/license/community.html) sob a licença Community para o contexto acadêmico. Em outro contexto de uso, confira a licença aplicável antes de publicar.

A atualização de um ativo usa o campo `versaoDoRegistro` para evitar que duas pessoas sobrescrevam alterações feitas ao mesmo tempo. Se houver conflito, a API orienta o usuário a recarregar os dados.

## Perfis de acesso

- **Administrador:** configura e opera o sistema, incluindo ações críticas e auditoria;
- **Gestor de TI:** opera os registros e pode executar ações críticas;
- **Operador de TI:** realiza os cadastros e as movimentações do dia a dia;
- **Auditor:** consulta os registros de auditoria.

O perfil **Colaborador** consulta apenas suas próprias entregas, devoluções e termos. O administrador inicial é criado pela configuração de bootstrap. Na edição de um colaborador ativo, o administrador pode criar a conta vinculada e definir o perfil. O e-mail de acesso é o usado na criação da conta e aparece na seção Conta de acesso.

## Recuperação de senha

Configure `Email:Servidor`, `Email:Porta`, `Email:Remetente` e `Email:UrlFrontend` (por exemplo, `http://localhost:3000` no desenvolvimento). Se o servidor SMTP exigir autenticação, inclua `Email:Usuario` e `Email:Senha` nos segredos do ambiente. O SMTP deve oferecer STARTTLS. O endereço externo do frontend deve usar HTTPS. Em produção com mais de uma instância, compartilhe as chaves do ASP.NET Core Data Protection entre as instâncias.

`Jwt:DuracaoMinutos` define a validade do JWT (60 minutos por padrão). Sessões emitidas antes da alteração de perfil, redefinição de senha ou inativação deixam de ser aceitas.

## Testes automatizados

O projeto `Orquestra.Testes` fica na solução e aparece no **Gerenciador de Testes** do Visual Studio. Pela linha de comando, rode `dotnet test Orquestra.slnx` na raiz para executar os testes unitários.

Para incluir os testes de integração, use o SQL Server LocalDB no Windows:

```powershell
sqllocaldb start MSSQLLocalDB
$env:ORQUESTRA_TESTES_SQL = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True'
dotnet test Orquestra.slnx
```

Os testes de integração usam a API real, aplicam as migrações em um banco com nome exclusivo e removem esse banco no fim da execução. Use uma instância de teste com permissão para criar bancos. A suíte usa contas fictícias e inclui cadastro e edição, permissões, inventário, entregas, devoluções, termos em PDF, desligamento e erros de recuperação de senha. A entrega de e-mail por SMTP deve ser validada em homologação.

## Homologação

O GitHub Actions compila a API, roda os testes unitários e valida se o modelo coincide com as migrações. Antes de colocar o sistema em uso, rode também a suíte de integração e valide o fluxo completo no ambiente de homologação.

Em produção, configure HTTPS e monitore o tempo de resposta e a disponibilidade. A meta do projeto é responder em até 3 segundos nas condições normais de uso e manter 99,5% de disponibilidade entre 07h e 20h em dias úteis. Agende backup diário do SQL Server com retenção de 30 dias e teste a restauração. Essas metas e rotinas dependem da infraestrutura escolhida e ainda precisam ser validadas no ambiente de destino.
