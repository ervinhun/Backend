# Graph Report - Backend  (2026-10-26)

## Corpus Check
- 63 files · ~8,411 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: (none) 7, .example 2)

## Summary
- 355 nodes · 859 edges · 13 communities (10 shown, 3 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 98 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `0e218e7b`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- http
- DataAccess.Entities
- Tests.csproj
- Task
- Fact
- CLAUDE.md
- Backend
- AppExceptionHandler
- AuthResponse
- AuthEndpointsTests.cs
- RevokedToken
- LocalDatabaseExtensions.cs
- JwtTokenService

## God Nodes (most connected - your core abstractions)
1. `User` - 33 edges
2. `AuthServiceTests` - 22 edges
3. `RefreshToken` - 21 edges
4. `AuthResponse` - 20 edges
5. `AuthService` - 17 edges
6. `DataAccess.Entities` - 17 edges
7. `AuthEndpointsTests` - 17 edges
8. `InMemoryTokenRepository` - 17 edges
9. `JwtTokenService` - 14 edges
10. `RevokedToken` - 14 edges

## Surprising Connections (you probably didn't know these)
- `AuthServiceTests` --calls--> `JwtTokenService`  [INFERRED]
  Tests/BusinessLogic/AuthServiceTests.cs → BusinessLogic/Services/JwtTokenService.cs
- `TestJwt` --references--> `JwtOptions`  [EXTRACTED]
  Tests/TestJwt.cs → BusinessLogic/Options/JwtOptions.cs
- `AuthService` --references--> `User`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Entities/User.cs
- `AuthService` --references--> `ITokenRepository`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Interfaces/ITokenRepository.cs
- `AuthService` --references--> `IUserRepository`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Interfaces/IUserRepository.cs

## Import Cycles
- None detected.

## Communities (13 total, 3 thin omitted)

### Community 0 - "http"
Cohesion: 0.12
Nodes (17): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+9 more)

### Community 1 - "DataAccess.Entities"
Cohesion: 0.06
Nodes (19): AppClaimTypes, NotFoundException, UserMapper, DataAccess.Entities, DataAccess.Interfaces, Api.Middleware, BusinessLogic.Exceptions, Tests.Fakes (+11 more)

### Community 2 - "Tests.csproj"
Cohesion: 0.08
Nodes (25): net10.0, net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, coverlet.collector (6.0.4), DotNetEnv (3.2.0), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13) (+17 more)

### Community 3 - "Task"
Cohesion: 0.09
Nodes (21): AppJwtBearerEvents, User, AuthProvider, CreatedAt, DefaultDisplayName, DeletedAt, Email, Id (+13 more)

### Community 4 - "Fact"
Cohesion: 0.20
Nodes (9): LoginRequest, Email, Password, RefreshRequest, RefreshToken, ConflictException, UnauthorizedException, AuthService (+1 more)

### Community 6 - "Backend"
Cohesion: 0.33
Nodes (5): Backend, Database migrations, Getting started, Project structure, Tech stack

### Community 8 - "AuthResponse"
Cohesion: 0.08
Nodes (7): AuthController, Program, AuthResponse, UserDto, IAuthService, AuthEndpointsTests, Factory

### Community 9 - "AuthEndpointsTests.cs"
Cohesion: 0.10
Nodes (7): RegisterRequest, DisplayName, Email, Locale, Password, BusinessLogic.Dtos.Auth, Tests.Api

### Community 10 - "RevokedToken"
Cohesion: 0.09
Nodes (18): AppDbContext, RefreshTokens, RevokedTokens, Users, RefreshToken, CreatedAt, ExpiresAt, FamilyId (+10 more)

### Community 12 - "JwtTokenService"
Cohesion: 0.10
Nodes (13): ConfigureJwtBearerOptions, ITokenService, JwtOptions, AccessTokenMinutes, Audience, Issuer, RefreshTokenDays, Secret (+5 more)

## Knowledge Gaps
- **64 isolated node(s):** `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)`, `Microsoft.AspNetCore.OpenApi (10.0.13)`, `Microsoft.EntityFrameworkCore.Design (10.0.13)` (+59 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 132 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `User` connect `Task` to `DataAccess.Entities`, `RevokedToken`, `Fact`, `JwtTokenService`?**
  _High betweenness centrality (0.088) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `User` (e.g. with `.LoginAsync()` and `.RegisterAsync()`) actually correct?**
  _`User` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)` to the rest of the system?**
  _64 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `http` be split into smaller, more focused modules?**
  _Cohesion score 0.11764705882352941 - nodes in this community are weakly interconnected._
- **Why does `JwtTokenService` connect `JwtTokenService` to `DataAccess.Entities`, `Fact`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **Should `DataAccess.Entities` be split into smaller, more focused modules?**
  _Cohesion score 0.05875706214689266 - nodes in this community are weakly interconnected._
- **Why does `DataAccess.Entities` connect `DataAccess.Entities` to `RevokedToken`?**
  _High betweenness centrality (0.059) - this node is a cross-community bridge._