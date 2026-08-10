# Review — Verificação de versões e fit tecnológico

**Lente:** cada tecnologia nomeada foi checada contra a web, ou foi asserida de memória?
**Veredito:** REPROVA no primeiro passe — 1 erro de compatibilidade, 4 versões asseridas sem verificação. Corrigidas.

## Achados

### V-1 — CRÍTICO — TypeScript 5.9 é incompatível com Angular 22

Spine fixava `TypeScript 5.9.x`. Angular 22 **exige TypeScript 6**; 5.9 e anteriores não são
suportados. O build quebra na primeira compilação.

**Correção aplicada:** `TypeScript 6.x`.

### V-2 — ALTO — Node.js 22 está em Maintenance LTS, não Active

Spine fixava `Node.js 22 LTS`. Em agosto/2026: Node 22 = Maintenance LTS, **Node 24 = Active LTS**,
Node 26 = Current (entra em LTS em outubro/2026). Angular 22 tem Node 22 como mínimo, mas começar
projeto novo em linha de manutenção encurta a vida do POC.

**Correção aplicada:** `Node.js 24 LTS`.

### V-3 — MÉDIO — DbUp "6.x" asserido de memória

Versão real do `dbup-sqlserver` no NuGet: **7.2.0**.

**Correção aplicada.**

### V-4 — MÉDIO — FluentValidation "12.x" impreciso

Versão real: **12.1.1**.

**Correção aplicada.**

### V-5 — MÉDIO — Microsoft.Data.SqlClient "6.x" desatualizado

Versão real: **7.0.2** (25/06/2026). Traz hardening de parsing TDS com checagem estrita de
limite de tamanho — relevante para um sistema que recebe código de barras bipado.

**Correção aplicada.**

### V-6 — BAIXO — SQL Server 2022 vs 2025

SQL Server 2025 chegou a GA em 18/11/2025 (build 17.0.1000.7), com imagem
`mcr.microsoft.com/mssql/server:2025-latest`.

**Mantido 2022 deliberadamente.** É POC de migração de legado; o risco a gastar é na tradução do
FoxPro, não na versão do banco. SQL Server 2022 tem suporte mainstream até 2028 e nada no spine usa
recurso exclusivo do 2025. Trade-off registrado no memlog — se o cliente já padroniza 2025, trocar
é mudança de uma linha no compose.

## Verificado e confirmado correto

| Item | Verificação |
| --- | --- |
| .NET 10 | LTS, GA 11/11/2025, suporte até 14/11/2028 |
| Dapper 2.1.79 | última em 16/05/2026, netstandard2.0 → roda em .NET 10 |
| Angular 22.1.x | release 03/06/2026, versão estável atual |
| C# 14 | acompanha .NET 10 |
| `PasswordHasher<T>` | existe em ASP.NET Core Identity, adequado para AD-7 |
| Dapper + DbUp | ambos independentes de EF Core; combinação documentada e usada |

## Fontes

- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/
- https://www.nuget.org/packages/Dapper/
- https://angular.dev/reference/versions
- https://nodejs.org/en/about/previous-releases
- https://www.nuget.org/packages/fluentvalidation/
- https://www.nuget.org/packages/dbup-sqlserver
- https://www.nuget.org/packages/Microsoft.Data.SqlClient/
- https://techcommunity.microsoft.com/blog/SQLServer/sql-server-2025-is-now-generally-available/4470570
