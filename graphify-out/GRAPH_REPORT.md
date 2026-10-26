# Graph Report - Backend  (2026-10-26)

## Corpus Check
- 66 files · ~8,925 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: (none) 7, .example 2)

## Summary
- 378 nodes · 916 edges · 21 communities (15 shown, 6 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 98 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `f5dea2a5`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- http
- Program.cs
- Tests.csproj
- Task
- Fact
- CLAUDE.md
- Backend
- User
- AuthResponse
- AuthEndpointsTests.cs
- CancellationToken
- LocalDatabaseExtensions.cs
- JwtTokenService
- AuthResponse
- BusinessLogic.Dtos.Auth
- JwtTokenService
- BusinessLogic.Exceptions
- JwtTokenService.cs
- AuthService.cs
- ConfigureJwtBearerOptions.cs
- UserMapper.cs

## God Nodes (most connected - your core abstractions)
1. `User` - 33 edges
2. `AuthServiceTests` - 22 edges
3. `RefreshToken` - 21 edges
4. `AuthResponse` - 20 edges
5. `InMemoryTokenRepository` - 18 edges
6. `AuthService` - 17 edges
7. `DataAccess.Entities` - 17 edges
8. `AuthEndpointsTests` - 17 edges
9. `JwtTokenService` - 14 edges
10. `RevokedToken` - 14 edges

## Surprising Connections (you probably didn't know these)
- `AuthServiceTests` --calls--> `JwtTokenService`  [INFERRED]
  Tests/BusinessLogic/AuthServiceTests.cs → BusinessLogic/Services/JwtTokenService.cs
- `AppJwtBearerEvents` --references--> `ITokenRepository`  [EXTRACTED]
  Api/Authentication/AppJwtBearerEvents.cs → DataAccess/Interfaces/ITokenRepository.cs
- `TestJwt` --references--> `JwtOptions`  [EXTRACTED]
  Tests/TestJwt.cs → BusinessLogic/Options/JwtOptions.cs
- `AuthService` --references--> `User`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Entities/User.cs
- `AuthService` --references--> `ITokenRepository`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Interfaces/ITokenRepository.cs

## Import Cycles
- None detected.

## Communities (21 total, 6 thin omitted)

### Community 0 - "http"
Cohesion: 0.12
Nodes (17): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+9 more)

### Community 1 - "Program.cs"
Cohesion: 0.21
Nodes (5): DataAccess.Entities, DataAccess.Interfaces, Tests.Fakes, DataAccess, DataAccess.Repositories

### Community 2 - "Tests.csproj"
Cohesion: 0.08
Nodes (25): net10.0, net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, coverlet.collector (6.0.4), DotNetEnv (3.2.0), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13) (+17 more)

### Community 3 - "Task"
Cohesion: 0.11
Nodes (11): UserDto, RevokedToken, CreatedAt, ExpiresAt, Jti, UserId, ITokenRepository, TokenRepository (+3 more)

### Community 4 - "Fact"
Cohesion: 0.15
Nodes (11): LoginRequest, Email, Password, RefreshRequest, RefreshToken, ConflictException, UnauthorizedException, AuthService (+3 more)

### Community 6 - "Backend"
Cohesion: 0.33
Nodes (5): Backend, Database migrations, Getting started, Project structure, Tech stack

### Community 7 - "User"
Cohesion: 0.09
Nodes (18): AppJwtBearerEvents, DataAccess.Exceptions, AuthProviders, User, AuthProvider, CreatedAt, DefaultDisplayName, DeletedAt (+10 more)

### Community 8 - "AuthResponse"
Cohesion: 0.13
Nodes (3): Program, AuthEndpointsTests, Factory

### Community 10 - "CancellationToken"
Cohesion: 0.11
Nodes (13): AppDbContext, RefreshTokens, RevokedTokens, Users, RefreshToken, CreatedAt, ExpiresAt, FamilyId (+5 more)

### Community 12 - "JwtTokenService"
Cohesion: 0.07
Nodes (15): ConfigureJwtBearerOptions, AppExceptionHandler, AuthRateLimitOptions, PermitLimit, WindowSeconds, ConfigureRateLimiterOptions, JwtOptions, AccessTokenMinutes (+7 more)

### Community 13 - "AuthResponse"
Cohesion: 0.20
Nodes (8): AuthController, AuthResponse, RegisterRequest, DisplayName, Email, Locale, Password, IAuthService

### Community 14 - "BusinessLogic.Dtos.Auth"
Cohesion: 0.14
Nodes (4): RateLimitPolicies, BusinessLogic.Dtos.Auth, Api.Controllers, Api.RateLimiting

### Community 15 - "JwtTokenService"
Cohesion: 0.22
Nodes (3): ITokenService, JwtTokenService, JwtTokenServiceTests

### Community 16 - "BusinessLogic.Exceptions"
Cohesion: 0.22
Nodes (3): NotFoundException, Api.Middleware, BusinessLogic.Exceptions

### Community 17 - "JwtTokenService.cs"
Cohesion: 0.22
Nodes (3): AppClaimTypes, BusinessLogic.Constants, BusinessLogic.Interfaces

## Knowledge Gaps
- **67 isolated node(s):** `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)`, `Microsoft.AspNetCore.OpenApi (10.0.13)`, `Microsoft.EntityFrameworkCore.Design (10.0.13)` (+62 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 130 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `JwtTokenService` connect `JwtTokenService` to `JwtTokenService.cs`, `JwtTokenService`, `Fact`?**
  _High betweenness centrality (0.083) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `User` (e.g. with `.LoginAsync()` and `.RegisterAsync()`) actually correct?**
  _`User` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)` to the rest of the system?**
  _67 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `http` be split into smaller, more focused modules?**
  _Cohesion score 0.11764705882352941 - nodes in this community are weakly interconnected._
- **Why does `User` connect `User` to `Task`, `Fact`, `CancellationToken`, `JwtTokenService`, `UserMapper.cs`?**
  _High betweenness centrality (0.082) - this node is a cross-community bridge._
- **Should `Tests.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.07816091954022988 - nodes in this community are weakly interconnected._
- **Why does `DataAccess.Entities` connect `Program.cs` to `Task`, `User`, `CancellationToken`, `JwtTokenService.cs`, `AuthService.cs`, `UserMapper.cs`?**
  _High betweenness centrality (0.057) - this node is a cross-community bridge._