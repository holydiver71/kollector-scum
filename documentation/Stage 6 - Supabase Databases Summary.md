# Stage 6 - Supabase Databases Summary

## Goal
Create separate staging and production PostgreSQL databases in Supabase and apply EF Core migrations safely (staging first).

## What Changed
- Added an EF Core design-time DbContext factory to allow `dotnet ef` to run without starting the full web host.
  - File: `backend/KollectorScum.Api/Data/KollectorScumDbContextFactory.cs`
- Added a small migration helper script that applies migrations using `ConnectionStrings__DefaultConnection`.
  - File: `backend/scripts/apply-ef-migrations.sh`
- Expanded Stage 6 in the cloud migration plan with concrete, repeatable commands.

## How To Use
0. Ensure the script is executable:
  - `chmod +x backend/scripts/apply-ef-migrations.sh`
0. Create a local secrets file:
  - `cp .env.example .env`
  - Set `KOLLECTOR_STAGING_DB_URL` and `KOLLECTOR_PROD_DB_URL` in `.env` (do not commit it)
1. Create Supabase projects:
   - `kollector-scum-staging`
   - `kollector-scum-prod`
2. Apply migrations:
  - Staging: `backend/scripts/apply-ef-migrations.sh --staging`
  - Production: `backend/scripts/apply-ef-migrations.sh --production`

## Notes
- Always apply and verify staging first.
- Keep production credentials scoped and stored as secrets in your deployment platform.
- If migrations fail with `Network is unreachable` and an IPv6 address, your Supabase DB hostname may be IPv6-only and your network lacks IPv6 routing. Use the Supabase connection pooling (session) hostname or run migrations from an IPv6-capable environment.