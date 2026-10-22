# Backend

FridgeFriend's backend API — authentication, fridges/containers, items, sharing, scanning and notifications.

Part of the [FridgeFriend](https://github.com/ervinhun/FridgeFriend) project. See that repo for the overall product plan, roadmap and tech stack; this repo covers only the backend.

## Getting started

1. Copy `.env.example` to `.env` and fill it in — it holds the database connection string.
2. Run with Docker:

   ```sh
   docker compose up --build
   ```

   or run the API directly:

   ```sh
   dotnet run --project Api
   ```

## Project structure

| Project | Contents |
|---|---|
| [`Api/`](Api/) | ASP.NET Core entry point: controllers, middleware, startup |
| [`BusinessLogic/`](BusinessLogic/) | Services, DTOs, validators, mappers |
| [`DataAccess/`](DataAccess/) | Entities and repositories |
| [`Tests/`](Tests/) | Unit/integration tests, mirroring the three projects above |

## Tech stack

- C# / .NET 10
- PostgreSQL, Flyway migrations
- Docker
- Sentry (monitoring)
