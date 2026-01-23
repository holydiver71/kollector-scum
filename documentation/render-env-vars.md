# Render Environment Variables — Kollector Scum API

This file lists the environment variables to add to the Render Web Service for staging and production. Copy values from your secrets manager.

## Required (per-environment)
- `ASPNETCORE_ENVIRONMENT` = `Staging` (staging) / `Production` (prod)
- `ConnectionStrings__DefaultConnection` = Postgres connection string (Supabase) for the environment
- `Jwt__Key` = 32+ char secret (mark as secret)
- `Jwt__Issuer` = your issuer
- `Jwt__Audience` = your audience
- `Frontend__Origins` = comma-separated frontend origins (e.g. https://app.example.com)

## Storage (choose R2 or Supabase per environment)
If using Supabase Storage:
- `Supabase__Url` = https://<project>.supabase.co
- `Supabase__AnonKey` = <anon-key> (mark as secret)
- (Optional, if your code needs service role key) `Supabase__ServiceRoleKey` = <service-role-key> (mark as secret)

If using Cloudflare R2:
- `R2__Endpoint` = https://<account>.r2.cloudflarestorage.com
- `R2__AccessKeyId` = <access-key-id> (mark as secret)
- `R2__SecretAccessKey` = <secret-access-key> (mark as secret)
- `R2__BucketName` = cover-art-staging OR cover-art-prod
- `R2__PublicBaseUrl` = optional public base URL (Worker or custom domain)

## Optional / feature flags
- `Discogs__Token` = <token> (if used)
- `LLM__ApiKey` = <key> (if used)
- `ASPNETCORE_URLS` = `http://+:8080` (usually not required; Render sets `PORT`)

## Local test / Docker run example (copy and fill values):

```bash
# from repo root
cd backend/KollectorScum.Api
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..." \
Supabase__Url="https://<supabase>.supabase.co" \
Supabase__AnonKey="<anon>" \
R2__Endpoint="https://<account>.r2.cloudflarestorage.com" \
R2__AccessKeyId="<id>" \
R2__SecretAccessKey="<secret>" \
R2__BucketName="cover-art-staging" \
dotnet run
```

## Quick checklist for Render UI
- Create Web Service → select repo `holydiver71/kollector-scum` → branch `dev` for staging
- Dockerfile path: `backend/Dockerfile`
- Health check path: `/health`
- Add all environment variables under Environment → Environment Variables (mark secrets)
- Deploy and then run migrations via a Render Job or the migration script as a one-off

## Notes & best practices
- Use separate keys for staging and production.
- Do NOT put service-role keys into frontend code or client-side envs.
- Rotate keys periodically and record rotation steps in your runbook.

---

File: documentation/render-env-vars.md
