# Board Game Library API

An ASP.NET Core 10 API for managing a board-game lending library. The solution demonstrates Clean Architecture, explicit business rules, PostgreSQL persistence, concurrency-safe loans, structured observability, deterministic demo data, and focused automated tests.

## What is included

- CRUD for board games, categories, physical copies, and members.
- Loan creation, history, filtering, and explicit return workflow.
- Literal case-insensitive search, bounded pagination, filtering, and deterministic sorting.
- PostgreSQL 18 through Docker Compose and EF Core migrations.
- Development seed with 17 categories, 120 games, 180 copies, 30 members, and 50 loans.
- RFC-compatible `ProblemDetails` responses with stable codes and trace identifiers.
- JSON console logs, liveness/readiness probes, and a Development/Testing OpenAPI document.
- Unit tests for domain and Application behavior, plus full HTTP/persistence tests against a disposable PostgreSQL Testcontainer.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). The repository's `global.json` selects an appropriate installed 10.0 feature band.
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) running with Linux containers.
- Git.
- Optional: Postman or Insomnia for the manual reviewer walkthrough.

The same workflow works on Windows and macOS. PostgreSQL does not need to be installed directly on the host.

## Fresh-clone quick start

Run these commands from the repository root. They are the same in PowerShell, Command Prompt, Bash, and zsh.

```shell
docker compose up -d --wait
dotnet tool restore
dotnet restore BoardGameLibrary.sln --locked-mode
dotnet ef database update --project src/BoardGameLibrary.Infrastructure --startup-project src/BoardGameLibrary.Api
dotnet dev-certs https --trust
dotnet run --project src/BoardGameLibrary.Api --launch-profile https
```

The database update applies the committed migration and runs the idempotent Development seed. The API deliberately does not apply migrations during normal startup.

Once the API is running:

- HTTPS API: `https://localhost:7080`
- HTTP endpoint: `http://localhost:5080` (redirects to HTTPS)
- OpenAPI JSON: `https://localhost:7080/openapi/v1.json`
- Liveness: `https://localhost:7080/health/live`
- Readiness: `https://localhost:7080/health/ready`

If your client does not trust the local certificate yet, run `dotnet dev-certs https --clean` followed by `dotnet dev-certs https --trust`, then restart the API. The certificate is local to the machine and is not committed to Git.

## Short reviewer walkthrough

After completing the quick start, these requests verify the main behavior without creating any data:

```shell
curl -k https://localhost:7080/health/ready
curl -k "https://localhost:7080/api/board-games?search=catan&players=4&isAvailable=true"
curl -k https://localhost:7080/api/board-games/0198a000-0002-7000-8000-000000000001
curl -k "https://localhost:7080/api/loans?status=active"
```

The fixed walkthrough identifiers are:

| Record | Identifier |
| --- | --- |
| Strategy category | `0198a000-0001-7000-8000-000000000001` |
| Catan board game | `0198a000-0002-7000-8000-000000000001` |
| `CATAN-001` copy | `0198a000-0003-7000-8000-000000000001` |
| `MEM-001` member | `0198a000-0004-7000-8000-000000000001` |
| Canonical active loan | `0198a000-0005-7000-8000-000000000029` |

For a more complete walkthrough, import [Board Game Library API.postman_collection.json](postman/Board%20Game%20Library%20API.postman_collection.json) into Postman or Insomnia. The collection uses the Postman Collection v2.1 format, which both clients support. Requests that return the canonical loan or create records intentionally change local demo state.

Import both files from the `postman` directory: `Board Game Library API.postman_collection.json` and `Board Game Library API.postman_environment.json`. In Postman, select the `Board Game Library API - Local` environment. In Insomnia, use **Import > From Folder** on the `postman` directory, then select the imported `Board Game Library API - Local` environment for the collection. Keep `baseUrl` as `https://localhost:7080`. The explicit environment makes every variable visible and editable in both clients, including the `created*` values captured during the walkthrough. The `API endpoints` folder is a standalone toolbox containing every route; edit `categoryId`, `boardGameId`, `gameCopyId`, `memberId`, and `loanId` as needed for live scenarios. For the scripted walkthrough, run `Health and contract`, `Seeded walkthrough`, `Write examples`, and `Error examples` in that order. Run the requests inside `Write examples` sequentially because its response scripts capture each created identifier for the next request.

Suggested flow:

1. Search, filter, sort, and paginate seeded games.
2. Create and update a category, game, copy, and member.
3. Create a loan for an eligible copy and observe that availability becomes `false`.
4. Return the loan and observe that availability becomes `true` again.
5. Try a second loan for that open copy (`409 game_copy_has_open_loan`).
6. Try to borrow with an overdue member, then with an inactive or damaged copy (rule-specific `409` responses).
7. Send an invalid request (`400`), request the documented missing UUID (`404`), and try the seeded protected delete (`409`).

The collection contains that order, the complete request bodies, and representative error cases. Its `Write examples` folder captures IDs from each `201` response and reuses them in the next request. Run that folder from top to bottom. It generates unique operational identifiers on each run.

Every error response contains `code` and `traceId`; the same trace value is returned in `X-Trace-Id` and appears in the structured console logs. This lets the reviewer correlate an HTTP failure without logging the request body or member personal data.

## Local database configuration

Compose starts `postgres:18.4-alpine3.24` on loopback only:

| Setting | Default |
| --- | --- |
| Host / port | `127.0.0.1:5432` |
| Database | `board_game_library` |
| Username | `boardgame` |
| Password | `boardgame_dev_password` |

These are disposable local-development credentials, not production secrets. Copy `.env.example` to `.env` to change Compose values.

The API runs outside Compose, so changing `.env` does not automatically reconfigure the .NET process. When changing the database values, also set `ConnectionStrings__BoardGameLibrary` in the shell that launches `dotnet`.

PowerShell:

```powershell
$env:ConnectionStrings__BoardGameLibrary = "Host=localhost;Port=55432;Database=my_board_games;Username=my_user;Password=my_password"
```

Bash/zsh:

```shell
export ConnectionStrings__BoardGameLibrary='Host=localhost;Port=55432;Database=my_board_games;Username=my_user;Password=my_password'
```

The configuration key is `BoardGameLibrary`. Do not commit real credentials.

### Migrations and seed

Restore the pinned local EF tool and apply migrations explicitly:

```shell
dotnet tool restore
dotnet ef database update --project src/BoardGameLibrary.Infrastructure --startup-project src/BoardGameLibrary.Api
```

The demo seed runs only in `Development`. It:

- is deterministic and safe to rerun;
- inserts missing records without overwriting edits;
- uses fixed UUIDv7 identifiers for the walkthrough subset;
- calculates open-loan dates relative to seed time so active and overdue examples remain useful;
- is disabled in integration tests.

### Reset local data

The following reset is destructive: it removes the local Compose database volume and all changes made to the development data.

```shell
docker compose down --volumes
docker compose up -d --wait
dotnet ef database update --project src/BoardGameLibrary.Infrastructure --startup-project src/BoardGameLibrary.Api
```

## Running tests

Docker Desktop must be running because integration tests create their own disposable PostgreSQL container.

```shell
dotnet test BoardGameLibrary.sln --configuration Release
```

The integration suite never uses the development database. It starts one PostgreSQL 18 Testcontainer for the suite, applies committed migrations, cleans application tables between scenarios, runs sequentially, and destroys the container afterward.

The test strategy is intentionally behavior-focused:

- unit tests cover domain invariants, state transitions, time rules, Result behavior, and Application use cases with small focused doubles;
- integration tests cover the real HTTP pipeline, EF mappings and queries, PostgreSQL constraints, migrations, seed behavior, and concurrency;
- controllers are not unit-tested in isolation, repository implementations are not mocked, and trivial DTO/property behavior is not tested.

No coverage percentage is used as a target. The goal is confidence in meaningful behavior rather than test count.

## HTTP contract

### Routes

```text
GET/POST        /api/board-games
GET/PUT/DELETE  /api/board-games/{id}
GET/POST        /api/categories
GET/PUT/DELETE  /api/categories/{id}
GET/POST        /api/board-games/{boardGameId}/copies
GET/PUT/DELETE  /api/game-copies/{id}
GET/POST        /api/members
GET/PUT/DELETE  /api/members/{id}
GET/POST        /api/loans
GET             /api/loans/{id}
POST            /api/loans/{id}/return
```

`POST` returns `201 Created`, a `Location` header, and `{ "id": "<uuid>" }`. `PUT` returns the complete updated representation. Deletes and loan return return `204 No Content`.

JSON uses camel case, string enums in camel case, `yyyy-MM-dd` civil dates, and ISO 8601 UTC timestamps.

### Pagination and sorting

Every collection accepts:

- `page` (default `1`);
- `pageSize` (default `20`, maximum `100`);
- `sortBy`;
- `sortDirection=asc|desc`.

Responses contain `items`, `page`, `pageSize`, `totalCount`, and `totalPages`. A page beyond the result returns an empty `items` array. Every ordering uses `id` in the same direction as a deterministic tie-breaker.

| Resource | Filters | Sort fields | Default |
| --- | --- | --- | --- |
| Board games | `search`, `categoryId`, `players`, `isAvailable`, `isActive` | `title`, `publisher`, `publicationYear`, `minPlayers`, `maxPlayers`, `playingTimeMinutes` | `title asc` |
| Categories | `search`, `isActive` | `name` | `name asc` |
| Copies | `condition`, `isAvailable`, `isActive` | `inventoryCode`, `condition`, `acquiredOn` | `inventoryCode asc` |
| Members | `search`, `isActive` | `fullName`, `memberNumber`, `joinedOn` | `fullName asc` |
| Loans | `memberId`, `gameCopyId`, `status`, `loanedFrom`, `loanedTo` | `loanedAtUtc`, `dueAtUtc`, `returnedAtUtc` | `loanedAtUtc desc` |

Omitting `isActive` returns active records for games, categories, copies, and members. Loans return all history by default. `loanedFrom` and `loanedTo` are inclusive UTC instants applied to `loanedAtUtc`.

Search is a case-insensitive substring match. `%`, `_`, and `\` are treated literally rather than as wildcard syntax. PostgreSQL `pg_trgm` GIN indexes support the searchable text fields.

### Errors

Expected failures use `application/problem+json`. Every response includes stable `code` and `traceId` fields; `X-Trace-Id` contains the corresponding trace value for log correlation.

| Status | Meaning |
| --- | --- |
| `400` | Invalid JSON contract, paging, filter, sort, or validation |
| `404` | Resource or referenced record not found |
| `409` | Uniqueness, state, dependency, loan-limit, or concurrency conflict |
| `500` | Generic unexpected failure without internal details |

Known PostgreSQL constraint violations are translated by SQLSTATE and constraint name, never by parsing server text.

## Important domain rules

- Availability is derived: the game and copy must be active, the copy cannot be damaged, and it cannot have an open loan.
- A member can hold at most three open loans and cannot borrow while any open loan is overdue.
- A loan lasts 14 days. An open loan has `returnedAtUtc == null`, including when overdue.
- Loan creation locks game, member, and copy rows in a fixed order and rechecks every eligibility rule inside a transaction.
- A partial unique PostgreSQL index prevents two open loans for one copy.
- Returning a loan locks its row; two simultaneous returns yield exactly one success and one conflict.
- Physical deletion is blocked when a record has dependents or loan history. Deactivation preserves historical data.

## Architecture

```text
BoardGameLibrary.Domain          <- business model and invariants
BoardGameLibrary.Application     <- use cases, Result types, and persistence ports
BoardGameLibrary.Infrastructure  <- EF Core, PostgreSQL, migrations, repositories, seed
BoardGameLibrary.Api             <- HTTP contracts, controllers, errors, logs, health, OpenAPI
```

Dependencies point inward. Controllers are thin and use explicit services/handlers. Mapping is manual. The project deliberately avoids MediatR, AutoMapper, generic repositories, and additional abstractions that would not earn their complexity in this assessment.

## Observability

- Console logs are structured JSON and include scopes with W3C trace/span identifiers.
- Successful write use cases are logged at Information; expected state conflicts at Warning; unexpected exceptions at Error.
- Request/response bodies, connection strings, credentials, email addresses, and phone numbers are not logged.
- `/health/live` checks only the process.
- `/health/ready` checks PostgreSQL and returns `503` when unavailable.
- The OpenAPI JSON document is exposed only in `Development` and `Testing`.

## Reproducibility and CI

- `global.json` pins the SDK policy.
- Central Package Management and committed lock files control dependencies.
- Nullable reference types and warnings-as-errors are enabled.
- The local `dotnet-ef` tool is pinned in `.config/dotnet-tools.json`.
- GitHub Actions restores in locked mode, builds Release, verifies no pending EF model changes, and runs all tests on Linux.

## Deliberate scope boundaries

Authentication, authorization, frontend UI, payments, notifications, cache, messaging, API versioning, production hosting, and full telemetry infrastructure are outside this assessment. The API itself is not containerized; Compose is used only for the local PostgreSQL dependency.
