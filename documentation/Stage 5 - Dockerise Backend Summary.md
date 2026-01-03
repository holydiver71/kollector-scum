# Stage 5 - Dockerise Backend Summary

## Goal
Dockerise the .NET 8 API so it can be deployed to a simple container host (e.g., Render) and correctly bind to the platform-provided `PORT`.

## What Changed
- Added a multi-stage Docker build at `backend/Dockerfile`.
- Added `backend/.dockerignore` to keep Docker build contexts small and fast.
- Updated the API startup to bind to `0.0.0.0:$PORT` when the `PORT` environment variable is set.

## How To Build & Run Locally
- Build: `docker build -f backend/Dockerfile -t kollector-scum-api backend`
- Development smoke test (boots without production-only config):
	- `docker run --rm -e PORT=8080 -e ASPNETCORE_ENVIRONMENT=Development -e "ConnectionStrings__DefaultConnection=Host=localhost;Database=kollector_scum;Username=postgres;Password=postgres" -p 8080:8080 kollector-scum-api`
- Staging/Production style run (you must provide real values):
	- `docker run --rm -e PORT=8080 -e ASPNETCORE_ENVIRONMENT=Production -e "ConnectionStrings__DefaultConnection=<real connection string>" -e "Frontend__Origins=<https://your-frontend-domain>" -e "Jwt__Key=<32+ char random secret>" -p 8080:8080 kollector-scum-api`
- Verify: `curl http://localhost:8080/health`

## Notes
- The container image exposes port 8080 by default (matching the .NET base image convention), but deployment platforms may set their own `PORT`. The API will bind to that automatically.
- Database migrations are intentionally not run automatically at startup; apply EF Core migrations during deployment.
- Static files (cover art) are served from `wwwroot/`. This repo includes an empty `wwwroot` folder to ensure container startup behaves consistently.