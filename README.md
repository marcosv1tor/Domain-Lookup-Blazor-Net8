
# Desafio Umbler

Esta é uma aplicação web que recebe um domínio e mostra suas informações de DNS.

Este é um exemplo real de sistema que utilizamos na Umbler.

Ex: Consultar os dados de registro do dominio `umbler.com`

**Retorno:**
- Name servers (ns254.umbler.com)
- IP do registro A (177.55.66.99)
- Empresa que está hospedado (Umbler)

Essas informações são descobertas através de consultas nos servidores DNS e de WHOIS.

*Obs: WHOIS (pronuncia-se "ruís") é um protocolo específico para consultar informações de contato e DNS de domínios na internet.*

Nesta aplicação, os dados obtidos são salvos em um banco de dados, evitando uma segunda consulta desnecessaria, caso seu TTL ainda não tenha expirado.

*Obs: O TTL é um valor em um registro DNS que determina o número de segundos antes que alterações subsequentes no registro sejam efetuadas. Ou seja, usamos este valor para determinar quando uma informação está velha e deve ser renovada.*

Tecnologias Backend utilizadas:

- C#
- Asp.Net Core
- MySQL
- Entity Framework

Tecnologias Frontend utilizadas:

- Blazor Server
- Razor Components

Para rodar o projeto você vai precisar instalar:

- dotnet Core SDK (https://www.microsoft.com/net/download/windows dotnet Core 6.0.201 SDK)
- Um editor de código, acoselhamos o Visual Studio ou VisualStudio Code. (https://code.visualstudio.com/)
- Um banco de dados MySQL (vc pode rodar localmente ou criar um site PHP gratuitamente no app da Umbler https://app.umbler.com/ que lhe oferece o banco Mysql adicionamente)

Com as ferramentas devidamente instaladas, basta executar os seguintes comandos:

Para Rodar o projeto:

Execute a migration no banco mysql:

`dotnet tool update --global dotnet-ef`
`dotnet tool ef database update`

E após: 

`dotnet run` (ou clique em "play" no editor do vscode)

# Objetivos:

Se você rodar o projeto e testar um domínio, verá que ele já está funcionando. Porém, queremos melhorar varios pontos deste projeto:

# FrontEnd

 - Os dados retornados não estão formatados, e devem ser apresentados de uma forma legível.
 - Não há validação no frontend permitindo que seja submetido uma requsição inválida para o servidor (por exemplo, um domínio sem extensão).
 - Está sendo utilizado "vanilla-js" para fazer a requisição para o backend, apesar de já estar configurado o webpack. O ideal seria utilizar algum framework mais moderno como ReactJs ou Blazor.  

# BackEnd

 - Não há validação no backend permitindo que uma requisição inválida prossiga, o que ocasiona exceptions (erro 500).
 - A complexidade ciclomática do controller está muito alta, o ideal seria utilizar uma arquitetura em camadas.
 - O DomainController está retornando a própria entidade de domínio por JSON, o que faz com que propriedades como Id, Ttl e UpdatedAt sejam mandadas para o cliente web desnecessariamente. Retornar uma ViewModel (DTO) neste caso seria mais aconselhado.

# Testes

 - A cobertura de testes unitários está muito baixa, e o DomainController está impossível de ser testado pois não há como "mockar" a infraestrutura.
 - O Banco de dados já está sendo "mockado" graças ao InMemoryDataBase do EntityFramework, mas as consultas ao Whois e Dns não. 

# Dica

- Este teste não tem "pegadinha", é algo pensado para ser simples. Aconselhamos a ler o código, e inclusive algumas dicas textuais deixadas nos testes unitários. 
- Há um teste unitário que está comentado, que obrigatoriamente tem que passar.
- Diferencial: criar mais testes.

# Entrega

- Enviei o link do seu repositório com o código atualizado.
- O repositório deve estar público para que possamos acessar..
- Modifique Este readme adicionando informações sobre os motivos das mudanças realizadas.

# Modificações:

## Modificações Realizadas

### 1) Arquitetura em camadas no backend
Foi aplicada uma separação por responsabilidades dentro do mesmo projeto:

- `Domain/Entities`: entidade `DomainRecord` para persistência.
- `Application/Contracts`: contratos de serviço, repositório, gateways externos e relógio (`IClock`).
- `Application/Services`: `DomainLookupService` com a regra de negócio (cache TTL, refresh externo, normalização).
- `Application/DTOs`: `DomainLookupResponseDto` para retorno enxuto da API.
- `Infrastructure/Persistence`: `DomainRepository` com EF Core.
- `Infrastructure/External`: wrappers `DnsLookupGateway` e `WhoisGateway` para DNS/Whois.
- `Web/Middleware`: middleware global para tratamento de exceções.

Benefícios:

- Redução da complexidade do controller.
- Lógica centralizada e testável sem dependência direta de infraestrutura externa.
- Facilita manutenção, evolução e injeção de dependências.

### 2) Controller e contrato de saída
`DomainController` foi refatorado para atuar como camada de apresentação:

- Recebe o domínio.
- Valida entrada.
- Chama `IDomainLookupService`.
- Retorna somente DTO para o frontend.

O endpoint foi mantido compatível:

- `GET /api/domain/{domainName}`

O retorno agora omite propriedades internas (`Id`, `Ttl`, `UpdatedAt`) e expõe:

- `domain`
- `ip`
- `hostedAt`
- `whois`
- `nameServers`
- `source` (`cache` ou `external`)

### 3) Validação e tratamento global de erros
Foram adicionadas validações robustas de domínio:

- Normalização (`Trim + lower`).
- Verificação de TLD obrigatório.
- Regex de domínio DNS válido.
- Validação adicional com `Uri.CheckHostName`.

Tratamento global de exceções:

- Erros de validação retornam `400 Bad Request` com `ProblemDetails`.
- Falhas inesperadas retornam `500 Internal Server Error` com mensagem genérica.

Também foi removido `EnableSensitiveDataLogging` fora de ambiente de desenvolvimento.

### 4) Regra de cache TTL
A regra foi corrigida e padronizada:

- Comparação em UTC: `UtcNow - UpdatedAt`.
- TTL interpretado em **segundos**.
- `TTL <= 0` força refresh externo.
- Domínio normalizado para busca consistente no cache.

### 5) Frontend migrado para Blazor Server
O frontend em Vanilla JS foi migrado para Blazor Server com componentes Razor:

- `Pages/DomainLookup.razor`
- `Components/DomainForm.razor`
- `Components/DomainResultCard.razor`
- `Components/ErrorAlert.razor`

Também foram aplicados:

- Validação de domínio no cliente (bloqueia envio inválido, ex.: `umbler` sem TLD).
- Mensagens de erro visuais.
- Estado de loading no botão.
- Exibição formatada e legível para IP, hostedAt, Name Servers e WHOIS.
- Consumo da API via `HttpClient` tipado (`IDomainApiClient`) usando a URL base dinâmica do `NavigationManager`.
- Estrutura Blazor completa com `_Imports.razor`, `App.razor` e host page `_Host.cshtml`.

### 6) Testes unitários e mockabilidade
A camada de aplicação foi desacoplada para permitir mocks de DNS/Whois.

Foram adicionados/ajustados testes para:

- Retorno por cache quando TTL é válido (sem chamadas externas).
- Refresh externo quando TTL expira.
- Rejeição de domínio inválido.
- Controller retornando DTO.
- **Teste de Whois antes comentado reativado**, agora passando com `IWhoisGateway` mockado.
- Cliente Blazor (`DomainApiClient`) cobrindo sucesso, erro 400 e erro 500.
- Validador de entrada da UI (`DomainInputValidator`) cobrindo domínio sem TLD e normalização.

Todos os testes com banco InMemory usam nome único por execução para evitar colisão.

## Como executar após a refatoração

### Aplicacao

```bash
dotnet tool update --global dotnet-ef
dotnet tool ef database update --project src/Desafio.Umbler
dotnet run --project src/Desafio.Umbler
```

### Testes

```bash
dotnet test
```
