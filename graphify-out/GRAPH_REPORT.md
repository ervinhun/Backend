# Graph Report - Backend  (2026-10-26)

## Corpus Check
- 55 files · ~6,242 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: (none) 7, .example 2)

## Summary
- 276 nodes · 537 edges · 11 communities (9 shown, 2 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 45 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `902fd912`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- http
- ServiceCollectionExtensions.cs
- Tests.csproj
- User
- Task
- CLAUDE.md
- Backend
- AppExceptionHandler.cs
- CancellationToken
- LocalDatabaseExtensions.cs
- JwtOptions

## God Nodes (most connected - your core abstractions)
1. `User` - 32 edges
2. `InMemoryUserRepository` - 13 edges
3. `AuthResponse` - 12 edges
4. `AuthService` - 12 edges
5. `DataAccess.Entities` - 12 edges
6. `AuthEndpointsTests` - 12 edges
7. `JwtTokenService` - 11 edges
8. `IUserRepository` - 11 edges
9. `AuthServiceTests` - 11 edges
10. `BusinessLogic.Dtos.Auth` - 10 edges

## Surprising Connections (you probably didn't know these)
- `TestJwt` --references--> `JwtOptions`  [EXTRACTED]
  Tests/TestJwt.cs → BusinessLogic/Options/JwtOptions.cs
- `AuthService` --references--> `User`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Entities/User.cs
- `AuthService` --references--> `IUserRepository`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Interfaces/IUserRepository.cs
- `AuthServiceTests` --calls--> `JwtTokenService`  [INFERRED]
  Tests/BusinessLogic/AuthServiceTests.cs → BusinessLogic/Services/JwtTokenService.cs
- `JwtTokenServiceTests` --references--> `User`  [EXTRACTED]
  Tests/BusinessLogic/JwtTokenServiceTests.cs → DataAccess/Entities/User.cs

## Import Cycles
- None detected.

## Communities (11 total, 2 thin omitted)

### Community 0 - "http"
Cohesion: 0.12
Nodes (17): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+9 more)

### Community 1 - "ServiceCollectionExtensions.cs"
Cohesion: 0.05
Nodes (20): Program, AppClaimTypes, UserMapper, DataAccess.Entities, DataAccess.Interfaces, BusinessLogic.Options, Tests.Fakes, BusinessLogic.Mappers (+12 more)

### Community 2 - "Tests.csproj"
Cohesion: 0.08
Nodes (25): net10.0, net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, coverlet.collector (6.0.4), DotNetEnv (3.2.0), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13) (+17 more)

### Community 3 - "User"
Cohesion: 0.07
Nodes (18): AppJwtBearerEvents, AppDbContext, Users, User, AuthProvider, CreatedAt, DefaultDisplayName, DeletedAt (+10 more)

### Community 4 - "Task"
Cohesion: 0.11
Nodes (10): LoginRequest, Email, Password, UnauthorizedException, ITokenService, AuthService, JwtTokenService, AuthEndpointsTests (+2 more)

### Community 6 - "Backend"
Cohesion: 0.33
Nodes (5): Backend, Database migrations, Getting started, Project structure, Tech stack

### Community 7 - "AppExceptionHandler.cs"
Cohesion: 0.10
Nodes (6): AppExceptionHandler, ConflictException, NotFoundException, Api.Middleware, BusinessLogic.Exceptions, Api.Controllers

### Community 8 - "CancellationToken"
Cohesion: 0.14
Nodes (9): AuthController, AuthResponse, RegisterRequest, DisplayName, Email, Locale, Password, UserDto (+1 more)

### Community 12 - "JwtOptions"
Cohesion: 0.15
Nodes (8): ConfigureJwtBearerOptions, JwtOptions, AccessTokenMinutes, Audience, Issuer, Secret, Tests, TestJwt

## Knowledge Gaps
- **46 isolated node(s):** `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)`, `Microsoft.AspNetCore.OpenApi (10.0.13)`, `Microsoft.EntityFrameworkCore.Design (10.0.13)` (+41 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 116 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `User` connect `User` to `ServiceCollectionExtensions.cs`, `Task`?**
  _High betweenness centrality (0.119) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `User` (e.g. with `.LoginAsync()` and `.RegisterAsync()`) actually correct?**
  _`User` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)` to the rest of the system?**
  _46 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `http` be split into smaller, more focused modules?**
  _Cohesion score 0.11764705882352941 - nodes in this community are weakly interconnected._
- **Why does `JwtTokenService` connect `Task` to `ServiceCollectionExtensions.cs`, `JwtOptions`?**
  _High betweenness centrality (0.071) - this node is a cross-community bridge._
- **Should `ServiceCollectionExtensions.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.051203277009728626 - nodes in this community are weakly interconnected._
- **Why does `DataAccess.Entities` connect `ServiceCollectionExtensions.cs` to `User`?**
  _High betweenness centrality (0.053) - this node is a cross-community bridge._