# Render Deployment Plan — Kollector Scum API

Goal: deploy the backend API to Render with a staging (dev) and production (main) workflow, ensure DB migrations run, environment variables and secrets are configured, and verify health/read/write against Supabase and R2.

**Prerequisites**
- GitHub repo access and Render account with permissions to connect repo.
- Supabase staging + prod connection strings and anon/service keys.
- Cloudflare R2 access keys for staging + prod (or Supabase bucket credentials if using Supabase for prod storage).
- `backend/Dockerfile` present (repo already includes one).
- Ensure `backend/scripts/apply-ef-migrations.sh` is executable and tested locally.

**Overview**
- Staging deploy: connect `dev` branch → Render Staging Web Service.
- Production deploy: connect `main` branch → Render Production Web Service.
- Use Dockerfile build (multi-stage) in `backend/`.
- Expose app port `8080` and configure health check to `/health`.

**Render service creation (staging)**
- In Render dashboard: New → Web Service.
- Connect GitHub and select this repo `holydiver71/kollector-scum`.
- Branch: `dev`.
- Environment: Docker.
- Dockerfile path: `/backend/Dockerfile` (or `backend/Dockerfile`).
- Build command: leave empty (using Dockerfile). Render will build the image.
- Start command: leave empty (Dockerfile `ENTRYPOINT` is used).
- Instance type: Free or Starter (choose according to expected traffic).
- Set the port: Render discovers exposed port 8080 automatically; ensure `PORT` env var is allowed.
- Health check path: `/health` (200 OK expected).

**Render service creation (production)**
- Repeat above but choose branch `main` and name the service `kollector-scum-api-prod`.
- Keep settings consistent with staging, but use production-sized instances only if needed.

**Environment variables & secrets (Render Dashboard)**
Create the following environment variables in the Render service settings (both staging and prod services), using Render "Environment" → "Environment Variables" (mark secrets where appropriate):
- `ASPNETCORE_ENVIRONMENT`: `Production` (Render will set for prod service; for staging you can use `Development` or `Staging`).
- `ConnectionStrings__DefaultConnection`: the Supabase Postgres connection string for the environment (staging/prod).
- `Jwt__Key`: production JWT secret (32+ chars).
- `Jwt__Issuer`, `Jwt__Audience`: as configured locally.
- `Supabase__Url`: https://<project>.supabase.co (staging/prod accordingly).
- `Supabase__AnonKey`: Supabase anon key (staging/prod accordingly).
- `R2__Endpoint`: https://<account>.r2.cloudflarestorage.com (if using R2)
- `R2__AccessKeyId`, `R2__SecretAccessKey`, `R2__BucketName`, `R2__PublicBaseUrl` (if using R2)
- `Frontend__Origin` or `Frontend__Origins`: staging and production frontend URLs for CORS.

Notes:
- Use Render secrets for keys; do NOT commit them.
- If using Supabase service-role key anywhere for signed URLs, store it securely and do not expose to clients.

**Database migrations**
Options (pick one):
- Prefer running migrations outside of the web service runtime (safer): use a Render Job or a CI step to run `backend/scripts/apply-ef-migrations.sh --staging` / `--production`.
- Alternative: run a one-off shell command in Render Dashboard (shell into instance) or use Render Cron Job to run the migration script once during deploy.

Recommended migration flow (staging)
1. Deploy service on `dev` (Render build completes but don't route traffic yet).
2. In Render Dashboard, open Shell for the new instance or create a temporary Job to run:

```bash
cd /srv/repo/backend/KollectorScum.Api || cd backend/KollectorScum.Api
chmod +x backend/scripts/apply-ef-migrations.sh
./backend/scripts/apply-ef-migrations.sh --staging
```

3. Verify migrations applied in Supabase → Database → Tables.
4. Start serving traffic (swap or enable the service as needed).

Production migration
- Run migrations only after staging is validated. Use the same script with `--production` and ensure you have DB backups.

**Post-deploy hooks / optional health tasks**
- Add a Post-Deploy Hook (CI or Render job) that pings `/health` until it returns 200.
- Optionally, run a smoke test script (curl endpoints, upload small image test) as a post-deploy job.

**Health checks**
- Set Render health check to `/health` returning HTTP 200.
- Logs are available via Render Logs; watch startup logs for errors connecting to Supabase or R2.

**Static files and `wwwroot`**
- No persistent `VOLUME` for `wwwroot/cover-art` required — object store (R2/Supabase) is the source.
- Keep `wwwroot` for other static assets if needed; the Dockerfile already publishes content.

**CI/CD: auto-deploy mapping**
- Set Render service for `dev` branch → auto-deploy on push to `dev`.
- Set Render service for `main` branch → auto-deploy on push to `main`.
- If using GitHub Actions, protect `main` with required status checks and PR review.

**Testing / verification checklist**
- After staging deploy:
  - `curl https://<staging-service>.onrender.com/health` → 200
  - GET `/api/releases` or relevant endpoint → returns data from Supabase
  - Upload a small test release (via frontend or cURL) and verify:
    - DB record created
    - File uploaded to R2/Supabase and public URL returned
  - Delete the test release and verify file removal in storage
- After production deploy: repeat checks, but start with a small sample and ensure DB backups are recent.

**Rollback plan**
- If deploy breaks, Render allows rollback to previous successful deploy from the service dashboard.
- If DB schema changes were applied and cause issues, restore DB from backup and roll back the code to compatible commit.
- Keep a documented runbook for DB restore steps.

**Monitoring & alerts**
- Configure Render alerts for service failures (build failures, instance errors).
- Enable Supabase DB backups and retention.
- Monitor R2/Supabase storage usage and set alerts for anomalous egress or size growth.

**Security & secrets**
- Rotate keys periodically.
- Use least-privilege keys for R2/Supabase where possible.
- Ensure CORS and `Frontend__Origin` restrict accepted origins.

**Advanced: run migrations during deploy (if needed)**
- If you must run migrations on deploy, implement a short-lived migration job that runs before the web service process starts. Prefer idempotent migrations and fail-safe checks.

**Commands & examples**
Build locally (optional):

```bash
# from repo root
docker build -f backend/Dockerfile -t kollector-scum-api backend
# run locally with staging envs
docker run --rm -e PORT=8080 -e ASPNETCORE_ENVIRONMENT=Development \
  -e "ConnectionStrings__DefaultConnection=<staging-conn>" \
  -e "Supabase__Url=https://<supabase>.supabase.co" \
  -e "Supabase__AnonKey=<anon>" \
  -e "R2__Endpoint=<r2-endpoint>" \
  -e "R2__AccessKeyId=<id>" \
  -e "R2__SecretAccessKey=<secret>" \
  -p 8080:8080 kollector-scum-api
```

Run migrations locally (script)

```bash
cd backend
chmod +x scripts/apply-ef-migrations.sh
./scripts/apply-ef-migrations.sh --staging
```

**Checklist before flipping prod traffic**
- [ ] Staging validation passed (health, reads, writes, uploads/deletes)
- [ ] DB schema migrations applied successfully
- [ ] Backups taken of production DB
- [ ] `main` branch protected and deploy config tested
- [ ] Monitoring/alerts configured

**Where to find key files in repo**
- `backend/Dockerfile`
- `backend/scripts/apply-ef-migrations.sh`
- `backend/KollectorScum.Api/Program.cs` (storage service registration)
- `backend/scripts/run-api-local.sh` (local helper)

---

If you want, I can also: add a Render `README` with exact screenshots and step-by-step button clicks, or create a GitHub Actions job that runs migrations as a pre-deploy step. Which would you prefer?