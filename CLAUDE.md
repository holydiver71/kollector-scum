# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Kollector Scum is a full-stack music collection catalog SaaS application. Users can catalog vinyl/CD releases, import from Discogs, and store cover art. It is invitation-only and fully multi-tenant.

**Stack:** .NET 8 C# Web API + Next.js 15 (App Router) + PostgreSQL (Supabase) + Cloudflare R2 (image storage) + Cloudflare Worker (image serving)

---

## Commands

### Backend

```bash
# Run API locally (against local PostgreSQL defined in appsettings.json)
./backend/scripts/run-api-local.sh --local

# Run against staging or production DB
./backend/scripts/run-api-local.sh --staging
./backend/scripts/run-api-local.sh --production

# Run all backend tests
dotnet test backend/KollectorScum.Tests

# Run a single test class
dotnet test backend/KollectorScum.Tests --filter "KollectorScum.Tests.Services.MusicReleaseServiceTests"

# Run a single test method
dotnet test backend/KollectorScum.Tests --filter "KollectorScum.Tests.Services.MusicReleaseServiceTests.MethodName"

# Apply EF migrations
backend/scripts/apply-ef-migrations.sh --staging
backend/scripts/apply-ef-migrations.sh --production
```

### Frontend

```bash
npm --prefix frontend ci          # Install dependencies
npm --prefix frontend run dev     # Dev server (uses Turbopack)
npm --prefix frontend run build
npm --prefix frontend run lint
npm --prefix frontend test                    # Jest unit tests
npm --prefix frontend run test:watch
npm --prefix frontend run test:coverage
npm --prefix frontend run test:e2e            # Playwright (requires running backend)
npm --prefix frontend run test:e2e:ui         # Playwright interactive mode
```

Frontend requires a `.env.local` file with:
```
NEXT_PUBLIC_GOOGLE_CLIENT_ID=...
NEXT_PUBLIC_API_BASE_URL=http://localhost:5072
```

### Docker / Full Stack

```bash
./start-docker-stack.sh           # Start full stack (PostgreSQL + API + Frontend)
cd backend && ./build-docker.sh   # Build backend Docker image
```

### Cloudflare Worker (R2 image server)

```bash
npm run deploy          # Deploy worker to production
npm run deploy:staging  # Deploy worker to staging
```

---

## Architecture

### Backend (Clean Architecture)

```
Controllers → Services → Repositories → EF Core DbContext → PostgreSQL
```

- **Controllers** inherit from `BaseApiController`. All endpoints require `[Authorize]`.
- **Services** contain business logic. `GenericCrudService<TEntity, TDto>` is the base for all CRUD services — it auto-scopes queries by `UserId` and supports pagination, search filtering, and 5-minute cache TTL for lookup tables.
- **Repositories** implement `IRepository<TEntity>` (async CRUD). `IUnitOfWork` coordinates transactions across repos.
- **DTOs** use FluentValidation for input validation; JSON is serialized as camelCase.
- **Middleware stack**: `ErrorHandlingMiddleware` → `SecurityHeadersMiddleware` → `ValidateUserMiddleware` (rate limiting + user validation).

### Multi-Tenancy

This is the most critical architectural decision. Every user-owned entity has a `UserId: Guid` field and implements `IUserOwnedEntity`.

- **Lookup tables** (Artist, Genre, Label, Format, Country, Packaging, Store) are **per-user**, not global. Composite unique constraint is `(UserId, Name)`, not just `Name`.
- `GenericCrudService` injects `IUserContext` and filters **all** queries by the current user's ID automatically.
- Join tables (e.g., `MusicReleaseArtist`) don't have `UserId`; isolation is enforced through FK chains.

### Authentication

- Google OAuth 2.0 → server-side token validation → JWT issued by the API.
- JWT claims: `NameIdentifier` (userId), `Email`, `IsAdmin` role.
- Invitation-only: a `UserInvitation` entry must exist and be marked used before access is granted.
- Admin impersonation: supported via `X-Admin-Act-As` header or `?userId=` query param (admin only).
- `IUserContext` service extracts the user ID from JWT claims throughout the service layer.

### Frontend

- Next.js 15 App Router. Routes in `app/`.
- `AuthContext` (React Context) manages auth state and JWT storage.
- `app/lib/api.ts` is the central Axios instance — it auto-injects the JWT `Authorization: Bearer` header on every request.
- No Redux/Zustand; auth state flows via context, component state via props.

### File Storage (Cloudflare R2)

- Cover art is stored in R2 at path: `cover-art-{staging|prod}/{userId}/{objectName}`
- Staging and production use **separate R2 buckets and credentials**.
- `CloudflareR2StorageService` handles uploads via the AWS S3-compatible SDK.
- A Cloudflare Worker (`worker/index.js`) serves R2 files publicly with cache headers.

### Discogs Integration

- Imports run **asynchronously** via a background job queue (`DiscogsImportJobQueue`).
- `DiscogsImportBackgroundService` processes jobs; status is tracked in the `DiscogsImportJob` entity.
- The frontend must poll for job status after triggering an import.
- Rate limit: 60 requests/minute to Discogs API (configurable in `appsettings.json`).

### Testing Approach

**Backend unit tests:** Moq for all dependencies; test services in isolation.

**Backend integration tests:** Use `WebApplicationFactory<Program>` with in-memory SQLite (not Postgres). A `TestAuthHandler` provides authenticated requests with claims. The test DB is seeded in `SeedDatabase()` within the fixture.

**Frontend unit tests:** Jest + React Testing Library.

**E2E:** Playwright against a running frontend + backend.

Target: 80%+ code coverage on the backend.

### Key Configuration Files

- `backend/KollectorScum.Api/appsettings.json` — local defaults (DB, JWT, Discogs, LLM/OpenAI, Google OAuth, SMTP)
- `backend/.env.example` — secrets template for staging/production (Postgres URLs, R2 credentials, etc.)
- `frontend/.env.local` (gitignored) — frontend env vars

### EF Core Migrations

Migrations live in `backend/KollectorScum.Api/Migrations/`. Use the design-time factory (`KollectorScumDbContextFactory`) when running `dotnet ef` commands. Apply via the helper scripts, not `dotnet ef database update` directly in CI/CD.

---

## Skill Files

AI agent skill files are maintained at:
- `.claude/skills/kollector-scum/SKILL.md` — for Claude Code (YAML frontmatter + body)
- `.github/copilot-instructions.md` — for GitHub Copilot coding agent (body only)

These files are evolved by the `gskill-kollector/` pipeline using the gskill recipe
(SWE-smith task generation + GEPA `optimize_anything` evolutionary refinement).

To regenerate the initial skill from source:
```bash
python gskill-kollector/generate_initial_skill.py
```

To run the full GEPA evolution loop (requires OPENAI_API_KEY and generated tasks):
```bash
python gskill-kollector/pipeline.py --max-evals 50 --tasks-dir gskill-kollector/tasks/
```

---

## Development Standards

- Follow SOLID principles and Clean Architecture layering — don't skip layers (e.g., controllers should not access repos directly).
- Add XML doc comments to all classes and methods.
- Keep Swagger documentation up to date for any new or changed endpoints.
- Each significant feature should have a phase summary `.md` written to `documentation/`.
- New branches per feature/phase; commit at the end of each task.
