# Graph Report - Backend  (2026-10-22)

## Corpus Check
- 53 files · ~6,010 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: (none) 7, .example 2)

## Summary
- 271 nodes · 528 edges · 14 communities (10 shown, 4 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 44 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `2ee6ae62`
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
- JwtOptions
- LocalDatabaseExtensions.cs

## God Nodes (most connected - your core abstractions)
1. `User` - 33 edges
2. `AuthService` - 13 edges
3. `InMemoryUserRepository` - 13 edges
4. `JwtTokenService` - 12 edges
5. `DataAccess.Entities` - 12 edges
6. `AuthResponse` - 11 edges
7. `AuthEndpointsTests` - 11 edges
8. `AuthServiceTests` - 11 edges
9. `BusinessLogic.Dtos.Auth` - 10 edges
10. `LoginRequest` - 10 edges

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

## Communities (14 total, 4 thin omitted)

### Community 0 - "http"
Cohesion: 0.12
Nodes (17): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+9 more)

### Community 1 - "ServiceCollectionExtensions.cs"
Cohesion: 0.08
Nodes (13): UserMapper, DataAccess.Entities, DataAccess.Interfaces, Tests.Fakes, BusinessLogic.Mappers, Tests.BusinessLogic, DataAccess, BusinessLogic.Dtos.Auth (+5 more)

### Community 2 - "Tests.csproj"
Cohesion: 0.08
Nodes (25): net10.0, net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, coverlet.collector (6.0.4), DotNetEnv (3.2.0), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13) (+17 more)

### Community 3 - "User"
Cohesion: 0.13
Nodes (15): UserDto, User, AuthProvider, CreatedAt, DefaultDisplayName, DeletedAt, Email, Id (+7 more)

### Community 4 - ".RegisterAsync"
Cohesion: 0.13
Nodes (8): AuthResponse, LoginRequest, Email, Password, ConflictException, UnauthorizedException, AuthEndpointsTests, AuthServiceTests

### Community 6 - "Backend"
Cohesion: 0.33
Nodes (5): Backend, Database migrations, Getting started, Project structure, Tech stack

### Community 7 - "AppExceptionHandler.cs"
Cohesion: 0.08
Nodes (8): AppExceptionHandler, Program, NotFoundException, Api.Middleware, BusinessLogic.Exceptions, DataAccess.Exceptions, DuplicateKeyException, Factory

### Community 9 - "JwtTokenService"
Cohesion: 0.14
Nodes (6): ServiceCollectionExtensions, IAuthService, ITokenService, AuthService, JwtTokenService, JwtTokenServiceTests

### Community 10 - ".Login"
Cohesion: 0.21
Nodes (6): AuthController, RegisterRequest, DisplayName, Email, Locale, Password

### Community 12 - "JwtOptions"
Cohesion: 0.22
Nodes (8): JwtOptions, AccessTokenMinutes, Audience, Issuer, Secret, BusinessLogic.Options, Tests, TestJwt

## Knowledge Gaps
- **46 isolated node(s):** `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)`, `Microsoft.AspNetCore.OpenApi (10.0.13)`, `Microsoft.EntityFrameworkCore.Design (10.0.13)` (+41 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 114 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `User` connect `User` to `JwtTokenService`, `AppDbContext`, `.RegisterAsync`, `ServiceCollectionExtensions.cs`?**
  _High betweenness centrality (0.126) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `User` (e.g. with `.LoginAsync()` and `.RegisterAsync()`) actually correct?**
  _`User` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)` to the rest of the system?**
  _46 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `http` be split into smaller, more focused modules?**
  _Cohesion score 0.11764705882352941 - nodes in this community are weakly interconnected._
- **Why does `JwtTokenService` connect `JwtTokenService` to `ServiceCollectionExtensions.cs`, `JwtOptions`, `.RegisterAsync`?**
  _High betweenness centrality (0.065) - this node is a cross-community bridge._
- **Should `ServiceCollectionExtensions.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.07575757575757576 - nodes in this community are weakly interconnected._
- **Why does `Api.Extensions` connect `LocalDatabaseExtensions.cs` to `ServiceCollectionExtensions.cs`, `AppExceptionHandler.cs`?**
  _High betweenness centrality (0.049) - this node is a cross-community bridge._