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

## 7) Multi-tenant storage & uploads (Cloudflare R2)

This section details how to implement multi-tenant cloud storage for cover art using Cloudflare R2, which is S3-compatible and offers zero-cost egress. Each user's files will be stored in a separate folder within a single R2 bucket, ensuring data isolation.

### 7.1 Create the Storage Service Interface

First, define an `IStorageService` interface to abstract file storage operations. This keeps the controller logic clean and independent of the specific storage provider.

Create `backend/KollectorScum.Api/Services/IStorageService.cs`:
```csharp
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace KollectorScum.Api.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(string bucketName, string userId, string fileName, Stream fileStream, string contentType);
        Task DeleteFileAsync(string bucketName, string userId, string fileName);
        string GetPublicUrl(string bucketName, string userId, string fileName);
    }
}
```

### 7.2 Implement the Cloudflare R2 Storage Service

Next, implement the interface with a service that interacts with Cloudflare R2 using the AWS S3 SDK.

**1. Install the AWS S3 SDK:**
```bash
cd backend/KollectorScum.Api
dotnet add package AWSSDK.S3
```

**2. Create `backend/KollectorScum.Api/Services/CloudflareR2StorageService.cs`:**
```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KollectorScum.Api.Services
{
    public class CloudflareR2StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<CloudflareR2StorageService> _logger;
        private readonly string _publicBaseUrl;

        public CloudflareR2StorageService(IConfiguration configuration, ILogger<CloudflareR2StorageService> logger)
        {
            _logger = logger;
            var accountId = configuration["R2:AccountId"];
            var accessKeyId = configuration["R2:AccessKeyId"];
            var secretAccessKey = configuration["R2:SecretAccessKey"];
            _publicBaseUrl = configuration["R2:PublicBaseUrl"]; // e.g., https://pub-....r2.dev

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                AuthenticationRegion = "auto",
            };

            _s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, config);
        }

        public async Task<string> UploadFileAsync(string bucketName, string userId, string fileName, Stream fileStream, string contentType)
        {
            try
            {
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
                var objectKey = $"{userId}/{uniqueFileName}";

                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    InputStream = fileStream,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.PublicRead // Required for public access
                };

                await _s3Client.PutObjectAsync(putRequest);
                return GetPublicUrl(bucketName, userId, uniqueFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to R2 for user {UserId}", userId);
                throw;
            }
        }

        public async Task DeleteFileAsync(string bucketName, string userId, string fileName)
        {
            try
            {
                var objectKey = $"{userId}/{Path.GetFileName(fileName)}";
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey
                };
                await _s3Client.DeleteObjectAsync(deleteRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file from R2 for user {UserId}", userId);
                throw;
            }
        }

        public string GetPublicUrl(string bucketName, string userId, string fileName)
        {
            // Assumes a public bucket URL is configured.
            // e.g., https://pub-your-hash.r2.dev/bucket-name/user-id/file-name
            var objectKey = $"{userId}/{Path.GetFileName(fileName)}";
            return $"{_publicBaseUrl}/{bucketName}/{objectKey}";
        }
    }
}
```

### 7.3 Register Service and Configure Environment

**1. Register the service** in `Program.cs`:
```csharp
// Add Cloudflare R2 Storage
builder.Services.AddScoped<IStorageService, CloudflareR2StorageService>();
```

**2. Add configuration** to `backend/KollectorScum.Api/appsettings.json`:
```json
{
  //... existing settings
  "R2": {
    "AccountId": "",
    "AccessKeyId": "",
    "SecretAccessKey": "",
    "BucketName": "cover-art",
    "PublicBaseUrl": ""
  }
}
```
These values will be overridden by environment variables in your hosting environments.

### 7.4 Update Release Controller for Cloud Storage

Modify `ReleasesController.cs` to use the `IStorageService`.

**1. Inject the service** into the controller's constructor:
```csharp
private readonly IStorageService _storageService;
private readonly string _bucketName;

public ReleasesController(
    CollectionContext context,
    ILogger<ReleasesController> logger,
    IStorageService storageService,
    IConfiguration configuration)
{
    _context = context;
    _logger = logger;
    _storageService = storageService;
    _bucketName = configuration["R2:BucketName"];
}
```

**2. Modify the upload logic:**
```csharp
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
if (string.IsNullOrEmpty(userId))
{
    return Unauthorized("User ID not found in token.");
}

using var stream = file.OpenReadStream();
coverArtUrl = await _storageService.UploadFileAsync(_bucketName, userId, file.FileName, stream, file.ContentType);
```

**3. Update the delete logic:**
```csharp
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
if (string.IsNullOrEmpty(userId))
{
    return Unauthorized("User ID not found in token.");
}

if (!string.IsNullOrEmpty(release.CoverArtUrl))
{
    var fileName = release.CoverArtUrl.Split('/').Last();
    await _storageService.DeleteFileAsync(_bucketName, userId, fileName);
}
```

### 7.5 Provision R2 Bucket and Set Environment Variables

**1. In the Cloudflare Dashboard:**
- Navigate to **R2** and create a bucket (e.g., `cover-art-staging`).
- Go to the bucket's **Settings** and find the **Public URL**. Copy this value.
- Go to **R2 → Manage R2 API Tokens** and create a new token with *Admin Read & Write* permissions. Note the **Access Key ID** and **Secret AccessKey**.

**2. In your hosting provider (Render):**
Set the following environment variables for your staging and production services:
```
R2__AccountId=YOUR_CLOUDFLARE_ACCOUNT_ID
R2__AccessKeyId=YOUR_R2_ACCESS_KEY_ID
R2__SecretAccessKey=YOUR_R2_SECRET_ACCESS_KEY
R2__BucketName=cover-art-staging # or cover-art-prod
R2__PublicBaseUrl=https://pub-your-hash.r2.dev # The public URL from bucket settings
```

### 7.6 Migration Strategy for Existing Cover Art

Create a one-time, admin-only API endpoint to migrate existing local files to R2.

**Add the migration endpoint** to a controller (protected by an admin role check):
```csharp
[HttpPost("migrate-storage-r2")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> MigrateToCloudStorage()
{
    var releasesToMigrate = await _context.Releases
        .Where(r => !string.IsNullOrEmpty(r.CoverArtUrl) && r.CoverArtUrl.StartsWith("/cover-art/"))
        .ToListAsync();

    // ... (implementation is similar to the Supabase one, but calls the R2 service)
    // See section 7.6 in the previous version for the full logic.
    // Ensure it uses the correct UserId for the R2 service `UploadFileAsync` method.
    
    return Ok(new { Message = "Migration logic to be implemented here." });
}
```

### 7.7 Update Dockerfile

Ensure the `Dockerfile` no longer relies on a volume for `wwwroot/cover-art/`.

**Before:**
```dockerfile
VOLUME ["/app/wwwroot/cover-art"]
```

**After:**
```dockerfile
# No volume needed for cover art; it is in Cloudflare R2.
```

### 7.8 Testing Checklist

**Local/Dev Testing**:
- [ ] Configure local environment variables to point to a staging R2 bucket.
- [ ] Upload a new release with cover art.
- [ ] Verify the image appears in the R2 bucket in the Cloudflare dashboard.
- [ ] Verify the public URL serves the image correctly.
- [ ] Edit/delete the release and verify the object is removed from R2.

**Staging/Production Testing**:
- [ ] Deploy the API and verify all `R2__*` environment variables are set.
- [ ] Test the full upload/edit/delete lifecycle.
- [ ] Run the migration endpoint (if needed) and verify old images are moved.

### 7.9 Image Processing and Backup

Before migrating, back up and optimize your existing images.

**1. Backup your originals**:
```bash
cp -a /home/andy/music-images /home/andy/music-images-backup-$(date +%Y%m%d)
```

**2. Install script dependencies**:
```bash
npm install --prefix backend/scripts sharp minimist
```

**3. Run resize simulation** (dry run):
```bash
node backend/scripts/resize-cover-images.js --path /home/andy/music-images/covers --max 1600 --dry
```

**4. Run the resize operation** (overwrites files):
```bash
node backend/scripts/resize-cover-images.js --path /home/andy/music-images/covers --max 1600
```

### 7.10 Rollback Plan

1.  **Revert Code**: Revert the commits related to `IStorageService` and restore the previous file system logic.
2.  **Redeploy**: Deploy the reverted version of the API.
3.  **Data Correction**: Manually update the `CoverArtUrl` in the database for any migrated releases, pointing them back to the local `/cover-art/{filename}` path.

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
