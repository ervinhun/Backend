# Backend

FridgeFriend's backend API — authentication, fridges/containers, items, sharing, scanning and notifications.

Part of the [FridgeFriend](https://github.com/ervinhun/FridgeFriend) project. See that repo for the overall product plan, roadmap and tech stack; this repo covers only the backend.

## Getting started

1. Copy `.env.example` to `.env` and fill it in — it holds the database connection string, and optionally the Google/Apple client ids that enable `POST /api/auth/external`.
2. Run with Docker:

   ```sh
   docker compose up --build
   ```

   or run the API directly:

   ```sh
   dotnet run --project Api
   ```

## Database migrations

Migrations live in [`db/migrations/`](db/migrations/) as versioned Flyway scripts (`V1__xxx.sql`, `V2__xxx.sql`, ...).

- **Local**: `docker compose up --build` runs them automatically — the `flyway` service migrates the local `db` container before `api` starts.
- **Production**: copy `.env.production.example` to `.env.production`, fill in the production database's connection info, then run:

  ```sh
  docker compose -f docker-compose.production.yml --env-file .env.production run --rm flyway
  ```

  This targets whatever `DB_URL` points at (e.g. a NeonDB connection string) instead of a local container — nothing else in `docker-compose.production.yml` is started.

## OpenAPI document

Every build writes [`openapi/FridgeFriend.json`](openapi/FridgeFriend.json). The app generates its API client from it with `swagger_parser`, so commit it with any change to an endpoint or DTO. If a PR's copy is out of date, CI commits the rebuilt file to the PR branch, so pull before pushing again. Each operation id is the controller action's name (`Login` → `AuthClient.login`), so action names must be unique across controllers — the build fails otherwise.

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
