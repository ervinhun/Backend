# Graph Report - Backend  (2026-10-26)

## Corpus Check
- 64 files · ~8,608 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: (none) 7, .example 2)

## Summary
- 370 nodes · 878 edges · 13 communities (10 shown, 3 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 91 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `50b208b9`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- http
- Tests.csproj
- Task
- Fact
- CLAUDE.md
- Backend
- AuthEndpointsTests.cs
- User
- LocalDatabaseExtensions.cs
- JwtTokenService
- Task
- JwtTokenService
- Program.cs

## God Nodes (most connected - your core abstractions)
1. `User` - 33 edges
2. `UserDevice` - 25 edges
3. `AuthServiceTests` - 23 edges
4. `AuthResponse` - 20 edges
5. `AuthService` - 17 edges
6. `AuthEndpointsTests` - 17 edges
7. `DataAccess.Entities` - 16 edges
8. `InMemoryUserDeviceRepository` - 15 edges
9. `JwtTokenService` - 14 edges
10. `InMemoryUserRepository` - 14 edges

## Surprising Connections (you probably didn't know these)
- `AuthServiceTests` --calls--> `JwtTokenService`  [INFERRED]
  Tests/BusinessLogic/AuthServiceTests.cs → BusinessLogic/Services/JwtTokenService.cs
- `AppJwtBearerEvents` --references--> `IUserRepository`  [EXTRACTED]
  Api/Authentication/AppJwtBearerEvents.cs → DataAccess/Interfaces/IUserRepository.cs
- `TestJwt` --references--> `JwtOptions`  [EXTRACTED]
  Tests/TestJwt.cs → BusinessLogic/Options/JwtOptions.cs
- `AuthService` --references--> `User`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Entities/User.cs
- `AuthService` --references--> `IUserDeviceRepository`  [EXTRACTED]
  BusinessLogic/Services/AuthService.cs → DataAccess/Interfaces/IUserDeviceRepository.cs

## Import Cycles
- None detected.

## Communities (13 total, 3 thin omitted)

### Community 0 - "http"
Cohesion: 0.12
Nodes (17): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, applicationUrl (+9 more)

### Community 2 - "Tests.csproj"
Cohesion: 0.08
Nodes (25): net10.0, net10.0, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, coverlet.collector (6.0.4), DotNetEnv (3.2.0), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13) (+17 more)

### Community 3 - "Task"
Cohesion: 0.10
Nodes (17): AppJwtBearerEvents, UserDevice, CreatedAt, DeletedAt, Id, IsActive, LastSeenAt, Platform (+9 more)

### Community 4 - "Fact"
Cohesion: 0.16
Nodes (10): LoginRequest, Email, Password, ConflictException, NotFoundException, UnauthorizedException, AuthService, BusinessLogic.Exceptions (+2 more)

### Community 6 - "Backend"
Cohesion: 0.33
Nodes (5): Backend, Database migrations, Getting started, Project structure, Tech stack

### Community 10 - "User"
Cohesion: 0.06
Nodes (23): Program, DataAccess.Exceptions, AppDbContext, UserDevices, Users, User, AuthProvider, CreatedAt (+15 more)

### Community 12 - "JwtTokenService"
Cohesion: 0.07
Nodes (15): ConfigureJwtBearerOptions, AppExceptionHandler, AuthRateLimitOptions, PermitLimit, WindowSeconds, ConfigureRateLimiterOptions, JwtOptions, AccessTokenMinutes (+7 more)

### Community 13 - "Task"
Cohesion: 0.09
Nodes (12): AuthController, AuthResponse, RefreshRequest, RefreshToken, RegisterRequest, DisplayName, Email, Locale (+4 more)

### Community 15 - "JwtTokenService"
Cohesion: 0.23
Nodes (3): ITokenService, JwtTokenService, JwtTokenServiceTests

### Community 17 - "Program.cs"
Cohesion: 0.05
Nodes (19): RateLimitPolicies, AppClaimTypes, UserMapper, DataAccess.Entities, DataAccess.Interfaces, Api.Middleware, Tests.Fakes, BusinessLogic.Mappers (+11 more)

## Knowledge Gaps
- **65 isolated node(s):** `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)`, `Microsoft.AspNetCore.OpenApi (10.0.13)`, `Microsoft.EntityFrameworkCore.Design (10.0.13)` (+60 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 129 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `JwtTokenService` connect `JwtTokenService` to `Program.cs`, `JwtTokenService`, `Fact`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `User` (e.g. with `.LoginAsync()` and `.RegisterAsync()`) actually correct?**
  _`User` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `net10.0`, `DotNetEnv (3.2.0)`, `Microsoft.AspNetCore.Authentication.JwtBearer (10.0.13)` to the rest of the system?**
  _65 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `http` be split into smaller, more focused modules?**
  _Cohesion score 0.11764705882352941 - nodes in this community are weakly interconnected._
- **Why does `User` connect `User` to `Task`, `Fact`, `Task`, `JwtTokenService`, `Program.cs`?**
  _High betweenness centrality (0.082) - this node is a cross-community bridge._
- **Should `Tests.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.07816091954022988 - nodes in this community are weakly interconnected._
- **Why does `AuthServiceTests` connect `Fact` to `Program.cs`, `User`, `Task`, `JwtTokenService`?**
  _High betweenness centrality (0.076) - this node is a cross-community bridge._