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

  The CD pipeline runs these migrations itself before every deploy, so this is only needed by hand.

## Release pipeline

`CI` → `Release Gates` → `CD`, each started when the one before it finishes (`workflow_run`):

| Push to | Release Gates | CD |
|---|---|---|
| `development` | builds the real image, migrates a throw-away Postgres, runs the image and checks [`scripts/e2e-facts.sh`](scripts/e2e-facts.sh) (including a sign-up); then migrates the **staging** Neon database, deploys to `fridgefriend-api-staging` and checks the facts there (read-only) | — |
| `main` | the same image + facts check | tags a release, migrates the **production** Neon database, deploys to `fridgefriend-api` and checks the facts there (read-only) |

`workflow_run` workflows run from the copy on the default branch (`main`), so changes to them take effect once they reach `main`.

Run the facts against any running API: `bash scripts/e2e-facts.sh http://localhost:5092` (add `WRITE=1` only for a throw-away database).

### One-time setup

1. Two Neon databases (staging and production), in `us-east-2`.
2. Two Fly apps, with their secrets:

   ```sh
   fly apps create fridgefriend-api && fly apps create fridgefriend-api-staging
   fly secrets set -a fridgefriend-api "ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...;SSL Mode=Require" "Jwt__Secret=$(openssl rand -base64 48)"
   # the same for fridgefriend-api-staging, with the staging database and a different secret
   ```

3. GitHub repository secrets (Settings → Secrets and variables → Actions):

   | Secret | Value |
   |---|---|
   | `FLY_API_TOKEN` | `fly tokens create org` |
   | `STAGING_DB_URL`, `PRODUCTION_DB_URL` | Neon JDBC URL, `jdbc:postgresql://<host>/<db>?sslmode=require` |
   | `STAGING_DB_USER`, `PRODUCTION_DB_USER` | database user |
   | `STAGING_DB_PASSWORD`, `PRODUCTION_DB_PASSWORD` | database password |

4. Optional: protect the `production` environment (Settings → Environments) to require an approval before migrating and deploying.

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
