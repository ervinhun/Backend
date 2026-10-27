# Graph Report - Backend  (2026-10-27)

## Corpus Check
- 63 files · ~8,792 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: (none) 7, .example 2)

## Summary
- 377 nodes · 892 edges · 20 communities (14 shown, 6 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 95 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `69cf6ae0`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- http
- Task
- Tests.csproj
- UserSession
- Fact
- CLAUDE.md
- Backend
- ConfigureRateLimiterOptions.cs
- AppExceptionHandler
- AuthEndpointsTests.cs
- BusinessLogic.Dtos.Auth
- LocalDatabaseExtensions.cs
- ConfigureRateLimiterOptions
- AuthResponse
- AuthController.cs
- RegisterRequest
- JwtTokenService.cs
- Program.cs
- JwtTokenService.cs
- Program.cs

## God Nodes (most connected - your core abstractions)
1. `User` - 33 edges
2. `UserSession` - 26 edges
3. `AuthServiceTests` - 24 edges
4. `AuthResponse` - 20 edges
5. `AuthService` - 18 edges
6. `AuthEndpointsTests` - 17 edges
7. `DataAccess.Entities` - 16 edges
8. `InMemoryUserSessionRepository` - 15 edges
9. `LoginRequest` - 14 edges
10. `JwtTokenService` - 14 edges

## Surprising Connections (you probably didn't know these)
- `AuthServiceTests` --calls--> `JwtTokenService`  [INFERRED]
  Tests/BusinessLogic/AuthServiceTests.cs → BusinessLogic/Services/JwtTokenService.cs
- `TestJwt` --references--> `JwtOptions`  [EXTRACTED]
  Tests/TestJwt.cs → BusinessLogic/Options/JwtOptions.cs
- `AuthService` --references--> `User`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Entities/User.cs
- `AuthService` --references--> `IUserRepository`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Interfaces/IUserRepository.cs
- `AuthService` --references--> `IUserSessionRepository`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Interfaces/IUserSessionRepository.cs

## Import Cycles
- None detected.

## Communities (20 total, 6 thin omitted)

### Community 0 - "http"
Cohesion: 0.12
Nodes (17): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+9 more)

### Community 1 - "Task"
Cohesion: 0.10
Nodes (19): AppJwtBearerEvents, User, AuthProvider, CreatedAt, DefaultDisplayName, DeletedAt, Email, Id (+11 more)

### Community 2 - "Tests.csproj"
Cohesion: 0.08
Nodes (25): net10.0, net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, coverlet.collector (6.0.4), DotNetEnv (3.2.0), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13) (+17 more)

### Community 3 - "UserSession"
Cohesion: 0.10
Nodes (16): AppDbContext, Users, UserSessions, UserSession, CreatedAt, DeletedAt, Id, IsActive (+8 more)

### Community 4 - "Fact"
Cohesion: 0.16
Nodes (10): LoginRequest, Email, Password, PhoneModel, Platform, ConflictException, UnauthorizedException, AuthService (+2 more)

### Community 6 - "Backend"
Cohesion: 0.33
Nodes (5): Backend, Database migrations, Getting started, Project structure, Tech stack

### Community 9 - "AuthEndpointsTests.cs"
Cohesion: 0.16
Nodes (3): Tests.Api, AuthRateLimitTests, WrongLogin

### Community 10 - "BusinessLogic.Dtos.Auth"
Cohesion: 0.19
Nodes (4): BusinessLogic.Mappers, BusinessLogic.Dtos.Auth, DataAccess.Exceptions, DuplicateKeyException

### Community 12 - "ConfigureRateLimiterOptions"
Cohesion: 0.08
Nodes (16): ConfigureJwtBearerOptions, AuthRateLimitOptions, PermitLimit, WindowSeconds, ConfigureRateLimiterOptions, ITokenService, JwtOptions, AccessTokenMinutes (+8 more)

### Community 13 - "AuthResponse"
Cohesion: 0.07
Nodes (10): AuthController, Program, AuthResponse, RefreshRequest, RefreshToken, UserDto, IAuthService, UserMapper (+2 more)

### Community 14 - "AuthController.cs"
Cohesion: 0.15
Nodes (4): RateLimitPolicies, Api.Middleware, Api.Controllers, Api.RateLimiting

### Community 15 - "RegisterRequest"
Cohesion: 0.29
Nodes (7): RegisterRequest, DisplayName, Email, Locale, Password, PhoneModel, Platform

### Community 16 - "JwtTokenService.cs"
Cohesion: 0.32
Nodes (4): AppClaimTypes, Tests.BusinessLogic, BusinessLogic.Services, BusinessLogic.Constants

### Community 17 - "Program.cs"
Cohesion: 0.21
Nodes (5): DataAccess.Entities, DataAccess.Interfaces, Tests.Fakes, DataAccess.Repositories, AuthProviders

## Knowledge Gaps
- **70 isolated node(s):** `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)`, `Microsoft.AspNetCore.OpenApi (10.0.13)`, `Microsoft.EntityFrameworkCore.Design (10.0.13)` (+65 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 134 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `JwtTokenService` connect `ConfigureRateLimiterOptions` to `JwtTokenService.cs`, `Fact`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `User` (e.g. with `.LoginAsync()` and `.RegisterAsync()`) actually correct?**
  _`User` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)` to the rest of the system?**
  _70 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `http` be split into smaller, more focused modules?**
  _Cohesion score 0.11764705882352941 - nodes in this community are weakly interconnected._
- **Why does `User` connect `Task` to `UserSession`, `Fact`, `ConfigureRateLimiterOptions`, `AuthResponse`, `Program.cs`?**
  _High betweenness centrality (0.082) - this node is a cross-community bridge._
- **Should `Task` be split into smaller, more focused modules?**
  _Cohesion score 0.09781420765027322 - nodes in this community are weakly interconnected._
- **Why does `AuthServiceTests` connect `Fact` to `JwtTokenService.cs`, `Task`, `UserSession`, `ConfigureRateLimiterOptions`?**
  _High betweenness centrality (0.080) - this node is a cross-community bridge._