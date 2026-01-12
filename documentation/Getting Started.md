# Getting Started

This document covers the common steps a new developer needs to run the project locally.

Prerequisites
- .NET SDK 8.x (install from https://dotnet.microsoft.com/) — confirm with `dotnet --version`.
- Node.js 20.x (use `nvm` to manage versions) and `npm` — confirm with `node --version` and `npm --version`.
- Optional: Docker (for running the full stack with `start-docker-stack.sh`).

Initial setup
1. Clone the repo and change to the project root.
2. Create a local `.env` file:

```bash
# Copy example file and edit values locally
cp .env.example .env

# OR, if you've exported secrets in your shell already, auto-populate:
backend/scripts/generate-env-from-shell.sh --force
```

Notes:
- The `.env` file is required by `backend/scripts/run-api-local.sh` when using `--staging` or `--production`.
- Keep real secrets out of git. Use GitHub Secrets for CI. If you must extract GitHub Secrets, follow an approved workflow.

Database and migrations
- If you have a Supabase DB, paste the provided connection URL(s) into `.env` using the `KOLLECTOR_STAGING_DB_URL` / `KOLLECTOR_PROD_DB_URL` keys.
- Encode DB URLs if needed: `backend/scripts/encode-db-urls-env.sh`.
- Apply EF migrations:

```bash
backend/scripts/apply-ef-migrations.sh --staging
# or --production or --local depending on target
```

Seeding sample data
- See `populate_data.sh` in the repo root or run the `KollectorScum.DataSeeder` project if available.

Running the backend
- From repo root, run the API (examples):

```bash
# Use local appsettings defaults
./backend/scripts/run-api-local.sh --local

# Use staging DB from .env
./backend/scripts/run-api-local.sh --staging
```

Running the frontend
- Install dependencies and run dev server:

```bash
cd frontend
npm ci
npm run dev
```

Or from the repo root:

```bash
npm --prefix frontend ci
npm --prefix frontend run dev
```

Docker
- To bring up the full stack via Docker (if configured):

```bash
./start-docker-stack.sh
```

Tests
- Backend unit tests:

```bash
dotnet test backend/KollectorScum.Tests
```

- Frontend unit tests:

```bash
npm --prefix frontend test
```

- Playwright E2E:

```bash
# install browsers (required once per machine)
npx playwright install
# run tests
npm --prefix frontend run test:e2e
```

OAuth & external services
- Google OAuth: create a project in Google Cloud Console and obtain `Google__ClientId` and `Google__ClientSecret`. Add these to `.env` for local testing.
- Cloudflare R2: if using cover art storage, set `R2__*` keys in `.env`.

Supabase (Postgres) setup
1. Create a Supabase project (https://app.supabase.com/) and choose a region. Note the project reference (shown in the dashboard URL and UI).
2. Get the DB connection URL:
	- In the Supabase project, go to Settings → Database → Connection string.
	- Copy the "Connection string (URI)" (it will be a `postgresql://...` URL).
	- Paste this into your `.env` as `KOLLECTOR_STAGING_DB_URL` or `KOLLECTOR_PROD_DB_URL` depending on the target.
3. Pooler vs direct connection:
	- For migrations and local development you may prefer a direct connection (the URI above).
	- If using Supabase's pooler, use the pooler connection string shown in the UI and be aware the username may be `postgres.<project-ref>` rather than `postgres`.
4. Network & access notes:
	- Supabase exposes a public endpoint protected by the DB password; ensure you keep credentials secret.
	- If you host a private DB, make sure the machine running migrations can reach it.
5. Apply EF migrations using the chosen connection string:

```bash
backend/scripts/apply-ef-migrations.sh --staging
```

6. If you copied a plain URL into `.env` and need to ensure correct URL-encoding, run the helper:

```bash
backend/scripts/encode-db-urls-env.sh
```

Cloudflare R2 setup (cover art storage)
1. Create an R2 bucket in the Cloudflare dashboard:
	- Go to R2 → Create bucket, choose a name (e.g. `cover-art-staging`).
2. Create an Access Key:
	- In the R2 section, open Access Keys and create a new key pair.
	- Copy the Access Key ID and Secret — you will not be able to see the secret again after creation.
3. Endpoint / Account ID:
	- Your R2 endpoint commonly looks like `https://<account_id>.r2.cloudflarestorage.com` or a custom domain. Copy the correct endpoint for your account.
4. CORS and public base URL:
	- If you serve cover art publicly, configure CORS and note a public base URL (set `R2__PublicBaseUrl` in `.env` if needed).
5. Add these entries to `.env` (example):

```dotenv
R2__AccountId=your-account-id
R2__Endpoint=https://<account_id>.r2.cloudflarestorage.com
R2__AccessKeyId=AKIA...
R2__SecretAccessKey=...
R2__BucketName=cover-art-staging
R2__PublicBaseUrl=https://...   # optional, if you serve files via a CDN or custom domain
```

Important
- Treat R2 access keys as secrets — do not commit them.
- For staging usage, you can populate these values into `.env` or export them into your shell and run `backend/scripts/generate-env-from-shell.sh --force`.


Troubleshooting
- Module not found (frontend): run `npm --prefix frontend ci` and retry. If persistent, remove `frontend/node_modules` and `.next` and reinstall.
- `.env` missing: copy `.env.example` or use `backend/scripts/generate-env-from-shell.sh`.
- Playwright issues: run `npx playwright install` to install browsers.
- Node / dotnet version mismatch: use `nvm` or the platform-specific SDK installers to match versions.

Security
- Never commit real secrets. `.env` is intentionally excluded from commits in this repo — treat any local `.env` as sensitive.
- For CI and GitHub Actions, use repository or organization Secrets instead of storing values in the repo.

Useful scripts and locations
- Backend run script: `backend/scripts/run-api-local.sh`
- Env generator: `backend/scripts/generate-env-from-shell.sh`
- Migrations: `backend/scripts/apply-ef-migrations.sh`
- DB URL encoder: `backend/scripts/encode-db-urls-env.sh`
- Docker helpers: `start-docker-stack.sh`, `stop-docker-stack.sh`

If anything here is unclear or you want me to include screenshots/short examples for obtaining Google/R2 credentials, I can expand the document.
