# Graph Report - Backend  (2026-10-26)

## Corpus Check
- 60 files · ~6,937 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: (none) 7, .example 2)

## Summary
- 306 nodes · 643 edges · 13 communities (10 shown, 3 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 55 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d992b909`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- http
- OidcExternalTokenValidator.cs
- Tests.csproj
- User
- Task
- CLAUDE.md
- Backend
- AppExceptionHandler
- CancellationToken
- AuthEndpointsTests.cs
- RevokedToken
- LocalDatabaseExtensions.cs
- JwtOptions

## God Nodes (most connected - your core abstractions)
1. `User` - 32 edges
2. `DataAccess.Entities` - 16 edges
3. `AuthEndpointsTests` - 15 edges
4. `AuthService` - 14 edges
5. `RevokedToken` - 14 edges
6. `AuthResponse` - 13 edges
7. `AuthServiceTests` - 13 edges
8. `InMemoryUserRepository` - 13 edges
9. `JwtTokenService` - 11 edges
10. `AppDbContext` - 11 edges

## Surprising Connections (you probably didn't know these)
- `TestJwt` --references--> `JwtOptions`  [EXTRACTED]
  Tests/TestJwt.cs → BusinessLogic/Options/JwtOptions.cs
- `AuthService` --references--> `User`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Entities/User.cs
- `AuthService` --references--> `IRevokedTokenRepository`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Interfaces/IRevokedTokenRepository.cs
- `AuthService` --references--> `IUserRepository`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Interfaces/IUserRepository.cs
- `AuthServiceTests` --calls--> `JwtTokenService`  [INFERRED]
  Tests/BusinessLogic/AuthServiceTests.cs → BusinessLogic/Services/JwtTokenService.cs

## Import Cycles
- None detected.

## Communities (13 total, 3 thin omitted)

### Community 0 - "http"
Cohesion: 0.12
Nodes (17): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+9 more)

### Community 1 - "OidcExternalTokenValidator.cs"
Cohesion: 0.05
Nodes (23): Program, AppClaimTypes, NotFoundException, UserMapper, DataAccess.Entities, DataAccess.Interfaces, BusinessLogic.Options, Api.Middleware (+15 more)

### Community 2 - "Tests.csproj"
Cohesion: 0.08
Nodes (25): net10.0, net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, coverlet.collector (6.0.4), DotNetEnv (3.2.0), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13) (+17 more)

### Community 3 - "User"
Cohesion: 0.11
Nodes (18): AppJwtBearerEvents, User, AuthProvider, CreatedAt, DefaultDisplayName, DeletedAt, Email, Id (+10 more)

### Community 4 - "Task"
Cohesion: 0.09
Nodes (11): LoginRequest, Email, Password, ConflictException, UnauthorizedException, ITokenService, AuthService, JwtTokenService (+3 more)

### Community 6 - "Backend"
Cohesion: 0.33
Nodes (5): Backend, Database migrations, Getting started, Project structure, Tech stack

### Community 8 - "CancellationToken"
Cohesion: 0.15
Nodes (4): AuthController, AuthResponse, UserDto, IAuthService

### Community 9 - "AuthEndpointsTests.cs"
Cohesion: 0.10
Nodes (7): RegisterRequest, DisplayName, Email, Locale, Password, BusinessLogic.Dtos.Auth, Tests.Api

### Community 10 - "RevokedToken"
Cohesion: 0.13
Nodes (9): AppDbContext, RevokedTokens, Users, RevokedToken, CreatedAt, ExpiresAt, Jti, UserId (+1 more)

### Community 12 - "JwtOptions"
Cohesion: 0.17
Nodes (8): ConfigureJwtBearerOptions, JwtOptions, AccessTokenMinutes, Audience, Issuer, Secret, Tests, TestJwt

## Knowledge Gaps
- **52 isolated node(s):** `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)`, `Microsoft.AspNetCore.OpenApi (10.0.13)`, `Microsoft.EntityFrameworkCore.Design (10.0.13)` (+47 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 119 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `User` connect `User` to `CancellationToken`, `OidcExternalTokenValidator.cs`, `RevokedToken`, `Task`?**
  _High betweenness centrality (0.104) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `User` (e.g. with `.LoginAsync()` and `.RegisterAsync()`) actually correct?**
  _`User` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)` to the rest of the system?**
  _52 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `http` be split into smaller, more focused modules?**
  _Cohesion score 0.11764705882352941 - nodes in this community are weakly interconnected._
- **Why does `JwtTokenService` connect `Task` to `OidcExternalTokenValidator.cs`, `RevokedToken`, `JwtOptions`?**
  _High betweenness centrality (0.066) - this node is a cross-community bridge._
- **Should `OidcExternalTokenValidator.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.05174825174825175 - nodes in this community are weakly interconnected._
- **Why does `DataAccess.Entities` connect `OidcExternalTokenValidator.cs` to `RevokedToken`, `Task`?**
  _High betweenness centrality (0.061) - this node is a cross-community bridge._