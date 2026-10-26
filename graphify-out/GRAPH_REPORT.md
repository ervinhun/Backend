# Graph Report - Backend  (2026-10-26)

## Corpus Check
- 54 files · ~6,206 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: (none) 7, .example 2)

## Summary
- 277 nodes · 543 edges · 12 communities (10 shown, 2 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 44 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `e17cbbce`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- http
- ServiceCollectionExtensions.cs
- Tests.csproj
- User
- .RegisterAsync
- CLAUDE.md
- Backend
- AppExceptionHandler.cs
- AuthEndpointsTests.cs
- JwtTokenService
- .Login
- AppDbContext

## God Nodes (most connected - your core abstractions)
1. `User` - 33 edges
2. `AuthService` - 13 edges
3. `InMemoryUserRepository` - 13 edges
4. `AuthResponse` - 12 edges
5. `JwtTokenService` - 12 edges
6. `DataAccess.Entities` - 12 edges
7. `IUserRepository` - 12 edges
8. `AuthEndpointsTests` - 12 edges
9. `AuthServiceTests` - 11 edges
10. `BusinessLogic.Dtos.Auth` - 10 edges

## Surprising Connections (you probably didn't know these)
- `AuthServiceTests` --calls--> `JwtTokenService`  [INFERRED]
  Tests/BusinessLogic/AuthServiceTests.cs → BusinessLogic/Services/JwtTokenService.cs
- `AuthController` --references--> `IAuthService`  [EXTRACTED]
  Api/Controllers/AuthController.cs → BusinessLogic/Interfaces/IAuthService.cs
- `TestJwt` --references--> `JwtOptions`  [EXTRACTED]
  Tests/TestJwt.cs → BusinessLogic/Options/JwtOptions.cs
- `AuthService` --references--> `User`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Entities/User.cs
- `AuthService` --references--> `IUserRepository`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Interfaces/IUserRepository.cs

## Import Cycles
- None detected.

## Communities (12 total, 2 thin omitted)

### Community 0 - "http"
Cohesion: 0.12
Nodes (17): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+9 more)

### Community 1 - "ServiceCollectionExtensions.cs"
Cohesion: 0.08
Nodes (13): AppClaimTypes, DataAccess.Entities, BusinessLogic.Options, Tests.Fakes, BusinessLogic.Mappers, Tests.BusinessLogic, DataAccess, BusinessLogic.Dtos.Auth (+5 more)

### Community 2 - "Tests.csproj"
Cohesion: 0.08
Nodes (25): net10.0, net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, coverlet.collector (6.0.4), DotNetEnv (3.2.0), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13) (+17 more)

### Community 3 - "User"
Cohesion: 0.08
Nodes (18): UserDto, UserMapper, DataAccess.Interfaces, DataAccess.Repositories, User, AuthProvider, CreatedAt, DefaultDisplayName (+10 more)

### Community 4 - ".RegisterAsync"
Cohesion: 0.14
Nodes (8): AuthResponse, LoginRequest, Email, Password, ConflictException, UnauthorizedException, AuthEndpointsTests, AuthServiceTests

### Community 6 - "Backend"
Cohesion: 0.33
Nodes (5): Backend, Database migrations, Getting started, Project structure, Tech stack

### Community 7 - "AppExceptionHandler.cs"
Cohesion: 0.06
Nodes (10): LocalDatabaseExtensions, AppExceptionHandler, Program, NotFoundException, Api.Middleware, Api.Extensions, BusinessLogic.Exceptions, DataAccess.Exceptions (+2 more)

### Community 9 - "JwtTokenService"
Cohesion: 0.19
Nodes (5): IAuthService, ITokenService, AuthService, JwtTokenService, JwtTokenServiceTests

### Community 10 - ".Login"
Cohesion: 0.21
Nodes (6): AuthController, RegisterRequest, DisplayName, Email, Locale, Password

### Community 12 - "AppDbContext"
Cohesion: 0.09
Nodes (10): ServiceCollectionExtensions, JwtOptions, AccessTokenMinutes, Audience, Issuer, Secret, Tests, AppDbContext (+2 more)

## Knowledge Gaps
- **46 isolated node(s):** `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)`, `Microsoft.AspNetCore.OpenApi (10.0.13)`, `Microsoft.EntityFrameworkCore.Design (10.0.13)` (+41 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 116 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `User` connect `User` to `JwtTokenService`, `.RegisterAsync`, `AppDbContext`, `ServiceCollectionExtensions.cs`?**
  _High betweenness centrality (0.123) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `User` (e.g. with `.LoginAsync()` and `.RegisterAsync()`) actually correct?**
  _`User` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)` to the rest of the system?**
  _46 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `http` be split into smaller, more focused modules?**
  _Cohesion score 0.11764705882352941 - nodes in this community are weakly interconnected._
- **Why does `JwtTokenService` connect `JwtTokenService` to `ServiceCollectionExtensions.cs`, `AppDbContext`, `.RegisterAsync`?**
  _High betweenness centrality (0.065) - this node is a cross-community bridge._
- **Should `ServiceCollectionExtensions.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.08292682926829269 - nodes in this community are weakly interconnected._
- **Why does `Api.Extensions` connect `AppExceptionHandler.cs` to `ServiceCollectionExtensions.cs`?**
  _High betweenness centrality (0.048) - this node is a cross-community bridge._