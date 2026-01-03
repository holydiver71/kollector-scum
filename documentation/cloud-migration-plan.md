# Cloud Migration Plan (v3) — Easy, Low-Ops, Mostly Free + CI/CD

**Goal:** Move your local webapp to the cloud so friends (<20 users) can access it, while keeping costs near-zero and deployments easy.

**Target end-state (recommended stack):**
- **Frontend (static SPA):** Cloudflare Pages (free)
- **Backend API (Docker):** Render Web Service (free tier; sleeps when idle)
- **Database:** Supabase Postgres — **two separate projects** (staging + production)
- **File/Image storage:** Supabase Storage — separate per environment
- **Auth:** Google OAuth (already implemented)

---

## 0) What You’ll End Up With
### Environments
**Staging**
- Frontend: Cloudflare Pages project (deploys from `dev`)
- API: Render service (deploys from `dev`)
- DB: Supabase project `*-staging`
- Storage: Supabase bucket(s) in staging project

**Production**
- Frontend: Cloudflare Pages project (deploys from `main`)
- API: Render service (deploys from `main`)
- DB: Supabase project `*-prod`
- Storage: Supabase bucket(s) in prod project

### Deploy control (“push often when you choose”)
- Push to `dev` → auto-deploys **staging**
- Merge `dev` → `main` → auto-deploys **production**

---

## 1) Prerequisites (Accounts + Local Tools)
### Accounts
- GitHub
- Supabase
- Render
- Cloudflare
- Google Cloud Console (OAuth)

### Local tools
- Git
- Docker
- .NET SDK (EF Core migrations)
- Node.js
- VS Code (or similar)

---

## 2) Repo Layout + Branch Strategy (CI/CD Foundation)
### Recommended monorepo layout
```
/frontend        # Frontend (Next.js app)
/backend         # Backend (.NET API, Dockerfile)
/documentation   # Docs, runbooks, migration plans
/.github         # CI workflows, PR templates, issue forms
```

### Branch strategy (simple, predictable)
- `dev` — active development; automatically deploys to staging.
- `main` — production-ready; deploys to production only.
- `feature/*` — short-lived feature branches merged into `dev` via PRs.
- `release/*` — optional release branches for preparing a release and running final QA.
- `hotfix/*` — critical fixes that are merged to `main` and back-merged to `dev`.

Rules and recommendations:
- Protect `main` and `dev` with branch protection (require PR review and passing CI).
- Use PR templates and issue/PR labels in `/.github/` to standardize contribution flow.
- Keep feature branches short-lived and rebase or merge regularly to avoid drift.
- Map deploys explicitly: `dev` -> Staging (Cloudflare Pages + Render staging), `main` -> Production.

---

## 3) Configuration via Environment Variables
Never hardcode secrets or URLs.

### Backend (.env.example)
- ConnectionStrings__DefaultConnection
- ASPNETCORE_ENVIRONMENT
- ASPNETCORE_URLS
- Jwt__Key
- Jwt__Issuer
- Jwt__Audience
- Google__ClientId
- Frontend__Origin (recommended for strict CORS; implement in API when locking down CORS)

Optional (feature-dependent):
- Discogs__Token
- LLM__ApiKey

Notes:
- This repo currently reads the database connection string from `ConnectionStrings:DefaultConnection` (not `DATABASE_URL`).
- If your hosting provider gives you a `DATABASE_URL`, map/convert it to `ConnectionStrings__DefaultConnection` in that platform's settings.

### Frontend (.env.example)
- NEXT_PUBLIC_API_BASE_URL
- NEXT_PUBLIC_GOOGLE_CLIENT_ID

Example values:
- `NEXT_PUBLIC_API_BASE_URL=https://your-api.example.com`
- `NEXT_PUBLIC_GOOGLE_CLIENT_ID=your-google-client-id.apps.googleusercontent.com`

---

## 4) Backend Readiness
- Configure strict CORS for staging + production frontend domains
- Add `GET /health` endpoint returning 200 OK

---

## 5) Dockerise Backend
- Add Dockerfile (multi-stage .NET build)
- Ensure app listens on `0.0.0.0:$PORT`
- Logs to stdout/stderr
- Optional: docker-compose for local dev only

Repo implementation:
- Dockerfile: `backend/Dockerfile`
- Build context: `backend/`

Local build:
- `docker build -f backend/Dockerfile -t kollector-scum-api backend`

Local run:
- Development smoke test (boots without production-only config):
	- `docker run --rm -e PORT=8080 -e ASPNETCORE_ENVIRONMENT=Development -e "ConnectionStrings__DefaultConnection=Host=localhost;Database=kollector_scum;Username=postgres;Password=postgres" -p 8080:8080 kollector-scum-api`
- Staging/Production style run (you must provide real values):
	- `docker run --rm -e PORT=8080 -e ASPNETCORE_ENVIRONMENT=Production -e "ConnectionStrings__DefaultConnection=<real connection string>" -e "Frontend__Origins=<https://your-frontend-domain>" -e "Jwt__Key=<32+ char random secret>" -p 8080:8080 kollector-scum-api`
- Health check: `curl http://localhost:8080/health`

Notes:
- The API serves static files (cover art) via `wwwroot/`. Ensure `wwwroot/` exists in the published output (this repo includes an empty `wwwroot` folder so container startup doesn’t warn).

---

## 6) Supabase: Staging + Production Databases
Create two Supabase projects:
- `kollector-scum-staging`
- `kollector-scum-prod`

### 6.1 Get connection strings
In each Supabase project:
- Go to **Project Settings → Database**
- Copy the **Connection string** (prefer the direct Postgres connection string for migrations)

Use it as:
- `ConnectionStrings__DefaultConnection` (backend)

### 6.2 Apply EF Core migrations (staging first)
This repo includes a design-time EF Core factory so you can run migrations without booting the full API host.

One-time (ensure script is executable):
- `chmod +x backend/scripts/apply-ef-migrations.sh`

### 6.2.1 Store staging + production DB secrets locally
Use a local secrets file at the repo root (this repo already gitignores `.env`).

1. Create your secrets file:
	- `cp .env.example .env`
2. Edit `.env` and set:
	- `KOLLECTOR_STAGING_DB_URL` (Supabase URL including the real password)
	- `KOLLECTOR_PROD_DB_URL` (Supabase URL including the real password)

Optional (URL-encode usernames/passwords in-place):
- `chmod +x backend/scripts/encode-db-urls-env.sh`
- `backend/scripts/encode-db-urls-env.sh`

Notes:
- Prefer using the exact Supabase URL they provide (it handles URL-encoding for special characters in passwords).
- Never commit `.env`.
- If you see `Network is unreachable` with an IPv6 address, your network likely lacks IPv6 routing. The migration script prefers IPv4 for `*.supabase.co` hostnames to avoid this.

Common production pitfall (pooler username):
- If you use a Supabase `*.pooler.supabase.com` connection string, the required username is often **not** just `postgres`.
- Copy/paste the exact pooler connection string Supabase provides (it commonly looks like `postgres.<project-ref>` as the username).
- If you hand-edit the username/password, you may hit `28P01: password authentication failed`.

Troubleshooting: Supabase host is IPv6-only
- Some Supabase `db.<ref>.supabase.co` hosts may resolve only to IPv6 (no `A` record). If your ISP/network cannot route IPv6, local migrations will fail.
- Fix options (pick one):
	- Use Supabase **Connection pooling** → **Session** connection string (often uses a `*.pooler.supabase.com` hostname which may have IPv4).
	- Run migrations from an IPv6-capable environment (e.g., GitHub Actions, Render shell, a different network).

Staging:
- `backend/scripts/apply-ef-migrations.sh --staging`

Verify staging API boots + `/health` returns 200.

Production (only after staging is verified):
- `backend/scripts/apply-ef-migrations.sh --production`

Notes:
- Migrations are intentionally not applied automatically at runtime.
- Keep staging and production as separate Supabase projects to reduce blast radius.
- Store the connection strings as secrets (GitHub Actions / Render / local secret manager), never in git.

### 6.3 Local vs staging vs production (switching databases)
You do NOT have to keep using a local database once staging exists, but it’s usually best for day-to-day development:
- Local DB: fast iteration, easy resets/seed data, no shared state.
- Staging DB: integration testing in a “prod-like” environment (networking, auth, hosting config).
- Production DB: avoid using for dev/testing; treat as read-only except controlled migrations/releases.

#### 6.3.1 What controls which DB the API uses?
The backend chooses its database based on `ConnectionStrings:DefaultConnection` (highest priority wins):
1. Environment variable: `ConnectionStrings__DefaultConnection`
2. `appsettings.{Environment}.json` (e.g. `appsettings.Development.json`)
3. `appsettings.json`

`ASPNETCORE_ENVIRONMENT` selects which `appsettings.{Environment}.json` is loaded (e.g. `Development`, `Staging`, `Production`).

#### 6.3.2 Switch backend DB while running locally
Local DB (default dev):
- Run with `ASPNETCORE_ENVIRONMENT=Development` and keep `ConnectionStrings:DefaultConnection` in `appsettings.Development.json` pointing to localhost.

Copy/paste (local DB):
- `cd backend/KollectorScum.Api && dotnet run`

Recommended (scripted):
- `backend/scripts/run-api-local.sh --local`

Staging DB (run API locally, connect to staging Supabase):
- Set environment variables when starting the API:
	- `ASPNETCORE_ENVIRONMENT=Development`
	- `ConnectionStrings__DefaultConnection=<staging connection string>`

Copy/paste (staging DB):
- `cd backend/KollectorScum.Api && ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__DefaultConnection="<staging-connection-string>" dotnet run`

Recommended (scripted, reads repo-root `.env`):
- `backend/scripts/run-api-local.sh --staging`

Production DB (run API locally, connect to prod Supabase — use with care):
- Same as staging, but use the production connection string.

Copy/paste (prod DB — use with care):
- `cd backend/KollectorScum.Api && ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__DefaultConnection="<prod-connection-string>" dotnet run`

Recommended (scripted, reads repo-root `.env` — use with care):
- `backend/scripts/run-api-local.sh --production`

#### 6.3.2.1 How to use `backend/scripts/run-api-local.sh`
This helper script starts the API locally while selecting the database target.

Common usage:
- Local DB (default dev DB from `appsettings.Development.json`):
	- `backend/scripts/run-api-local.sh --local`
- Staging DB (reads `.env` → `KOLLECTOR_STAGING_DB_URL`):
	- `backend/scripts/run-api-local.sh --staging`
- Production DB (reads `.env` → `KOLLECTOR_PROD_DB_URL`, use with care):
	- `backend/scripts/run-api-local.sh --production`

Optional flags:
- Run on a different port:
	- `backend/scripts/run-api-local.sh --staging --port 8081`
- Override ASP.NET environment (defaults to `Development`):
	- `backend/scripts/run-api-local.sh --staging --environment Development`

Output:
- The script prints the listening URL + mode, but never prints the DB connection string.

Tip: keep staging/prod URLs in the repo-root `.env` (gitignored) and use `backend/scripts/apply-ef-migrations.sh` for migrations.
For running the API, you can export the chosen `ConnectionStrings__DefaultConnection` from your secret manager or shell.

#### 6.3.3 Switch what the frontend talks to
The frontend does not connect to Postgres directly; it calls the API.
Switching “environment” in the frontend is typically just changing:
- `NEXT_PUBLIC_API_BASE_URL` to either:
	- local API (e.g. `http://localhost:5072`) for local dev
	- staging API URL (Render staging service) for staging
	- production API URL (Render production service) for production

Copy/paste (frontend -> local API):
- `cd frontend && NEXT_PUBLIC_API_BASE_URL=http://localhost:5072 npm run dev`

Copy/paste (frontend -> staging API):
- `cd frontend && NEXT_PUBLIC_API_BASE_URL=https://<your-staging-api-host> npm run dev`

Copy/paste (frontend -> production API):
- `cd frontend && NEXT_PUBLIC_API_BASE_URL=https://<your-production-api-host> npm run dev`

If your backend runs in `Development`, CORS defaults to `AllowAnyOrigin()`.
In `Staging`/`Production`, you must set `Frontend__Origin` or `Frontend__Origins` so CORS is explicitly configured.

### 6.4 Populate staging with local/dev data (optional)
If you want staging to contain the same data as your local/dev database, you can copy **data-only** from your dev DB into staging.

Pre-req:
- Apply EF migrations to staging first (schema must exist).

WARNING:
- This truncates all data in staging `public` schema (except `__EFMigrationsHistory`) before importing.

One-time:
- `chmod +x backend/scripts/copy-dev-db-to-staging.sh`

Run:
- `backend/scripts/copy-dev-db-to-staging.sh --yes`

Overrides (if needed):
- `backend/scripts/copy-dev-db-to-staging.sh --yes --source "<local connection string or postgresql:// URL>"`
- `backend/scripts/copy-dev-db-to-staging.sh --yes --staging "<staging postgresql:// URL>"`

Defaults:
- Source DB: `backend/KollectorScum.Api/appsettings.Development.json` → `ConnectionStrings:DefaultConnection`
- Staging DB: repo-root `.env` → `KOLLECTOR_STAGING_DB_URL`

### 6.5 If you can’t see data after copy (multi-tenant ownership)
The API is multi-tenant: most data is scoped by `UserId` from your JWT. If you copied data from dev/local into staging, that data may be owned by the seeded “admin” user (from the multi-tenant migration) rather than your Google user. In that case, signing in as your Google account will show an empty collection even though the tables contain rows.

Fix (staging only): reassign ownership from the seeded admin user to your user.

Pre-reqs:
- You must sign in once (creates your `ApplicationUsers` row in staging).
- Repo-root `.env` must contain `KOLLECTOR_STAGING_DB_URL`.

Run:
- `backend/scripts/assign-staging-data-to-user.sh --email <your-google-email> --yes`

### 6.6 Production: apply migrations + populate with your user data

This section folds in lessons from staging so production setup is smoother.

#### 6.6.1 Apply migrations to production
- Ensure repo-root `.env` has `KOLLECTOR_PROD_DB_URL`.
- Run:
	- `backend/scripts/apply-ef-migrations.sh --production`

#### 6.6.2 Populate production from staging (bootstrap only)

DANGER: this wipes production data before importing.

Recommended approach (copy staging → production, data-only):

1. Ensure staging has exactly the data you want in production.
2. Run:
	- `chmod +x backend/scripts/copy-staging-db-to-production.sh`
	- `backend/scripts/copy-staging-db-to-production.sh --yes-i-understand-this-will-delete-production-data`

Why copy from staging instead of local/dev?
- Staging is already “prod-like” (same schema/migrations, same Supabase features).
- Avoids surprises from local-only seed data.

#### 6.6.3 Ensure the data is owned by your production user

Lesson from staging: copied rows may be owned by the *empty GUID* (`00000000-0000-0000-0000-000000000000`) or by the seeded admin user, so your Google login won’t see any data until ownership is reassigned.

1. Sign in once via the production frontend (creates your `ApplicationUsers` row).
2. Diagnose ownership:
	- `backend/scripts/assign-db-data-to-user.sh --production --email <your-google-email> --diagnose`
3. Reassign from the empty GUID (most common after raw data copy):
	- `backend/scripts/assign-db-data-to-user.sh --production --email <your-google-email> --yes --from-user-id 00000000-0000-0000-0000-000000000000`

If the diagnosis shows a different owner UUID, rerun the reassignment using that value.

---

## 7) Supabase Storage
Create identical buckets in both environments (e.g. `cover-art`).
Store only image metadata in DB, not blobs.

---

## 8) Render Deployment
Create two services:
- API staging → branch `dev`
- API production → branch `main`

Set env vars per environment:
- ConnectionStrings__DefaultConnection
- Jwt__Key
- Google__ClientId
- Frontend__Origin

Optional (feature-dependent):
- Discogs__Token
- LLM__ApiKey

---

## 9) Cloudflare Pages Deployment
Create two projects:
- Frontend staging → branch `dev`
- Frontend production → branch `main`

Set env vars:
- NEXT_PUBLIC_API_BASE_URL
- NEXT_PUBLIC_GOOGLE_CLIENT_ID

---

## 10) Google OAuth Configuration
Add both staging and production domains to:
- Authorized JavaScript origins
- Authorized redirect URIs (if using redirect flow)

---

## 11) CI/CD Flow
- Push to `dev` → staging auto-deploy
- Merge to `main` → production auto-deploy
- Optional GitHub Actions for build/test checks

---

## 12) Release Checklist
1. Push changes to `dev`
2. Verify staging works
3. Apply EF Core migrations to staging (if needed)
4. Merge `dev` → `main`
5. Apply EF Core migrations to production (if needed)
6. Smoke test production

---

## 13) Rollback
- Revert commit in `main`
- Hosting platforms redeploy last good state

---

## Summary
This setup gives you:
- Separate staging/production environments
- Safe, frequent releases
- Mostly free hosting
- Minimal operational overhead
