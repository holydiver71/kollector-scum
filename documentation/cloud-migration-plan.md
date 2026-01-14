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

### 6.3 Initial production data migration (one-time setup)

For your initial production launch, you'll need to migrate your existing collection data from local/staging to production. This is a **one-time operation** - after this, staging and production maintain separate data.

**📖 Detailed walkthrough:** See [section-6.3-production-setup.md](./section-6.3-production-setup.md) for complete step-by-step instructions.

**Quick summary:**

**Option 1: pg_dump/pg_restore (Recommended for initial migration)**

Export from staging:
```bash
pg_dump "$KOLLECTOR_STAGING_DB_URL" \
  --data-only \
  --no-owner \
  --no-privileges \
  --exclude-table-data=__EFMigrationsHistory \
  -f staging_data.sql
```

Import to production (after schema migrations applied):
```bash
psql "$KOLLECTOR_PROD_DB_URL" -f staging_data.sql
```

**Option 2: Supabase dashboard export/import**
1. Go to staging Supabase project → Database → Backups
2. Create a manual backup or download existing backup
3. Go to production Supabase project → Database → Backups  
4. Import the backup (note: this will include schema, adjust if schema already exists)

**Option 3: Application-level data seeder (for controlled, selective migration)**
- Create a one-time data migration endpoint in your API
- Read from staging DB connection
- Write to production DB connection
- Allows filtering/transforming data during migration

**Important notes:**
- Run production schema migrations (`apply-ef-migrations.sh --production`) **before** importing data
- Backup staging database before export
- Test the import on a **non-production copy** of the database first
- Verify user IDs, timestamps, and foreign key relationships after import
- After migration, staging and production evolve independently (no ongoing sync)

### 6.4 Local vs staging vs production (switching databases)
You do NOT have to keep using a local database once staging exists, but it’s usually best for day-to-day development:
- Local DB: fast iteration, easy resets/seed data, no shared state.
- Staging DB: integration testing in a “prod-like” environment (networking, auth, hosting config).
- Production DB: avoid using for dev/testing; treat as read-only except controlled migrations/releases.

#### 6.4.1 What controls which DB the API uses?
The backend chooses its database based on `ConnectionStrings:DefaultConnection` (highest priority wins):
1. Environment variable: `ConnectionStrings__DefaultConnection`
2. `appsettings.{Environment}.json` (e.g. `appsettings.Development.json`)
3. `appsettings.json`

`ASPNETCORE_ENVIRONMENT` selects which `appsettings.{Environment}.json` is loaded (e.g. `Development`, `Staging`, `Production`).

#### 6.4.2 Switch backend DB while running locally
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

## 7) Multi-tenant storage & uploads

```csharp
                fileStream,
                objectPath,
                new Supabase.Storage.FileOptions { ContentType = contentType, Upsert = false }
            );

            return GetPublicUrl(bucketName, userId, uniqueFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} for user {UserId}", fileName, userId);
            throw;
        }
    }

    public async Task DeleteFileAsync(string bucketName, string userId, string fileName)
    {
        try
        {
            var client = new SupabaseStorageClient(_supabaseUrl, _supabaseKey);
            var bucket = client.From(bucketName);
            
            var objectPath = $"{userId}/{Path.GetFileName(fileName)}";
            
            await bucket.Remove(new List<string> { objectPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileName} for user {UserId}", fileName, userId);
            throw;
        }
    }

    public string GetPublicUrl(string bucketName, string userId, string fileName)
    {
        var objectPath = $"{userId}/{Path.GetFileName(fileName)}";
        return $"{_supabaseUrl}/storage/v1/object/public/{bucketName}/{objectPath}";
    }
}
```

**Register the service** in `Program.cs`:

```csharp
// Add Supabase Storage
builder.Services.AddScoped<IStorageService, SupabaseStorageService>();
```

**Update environment configuration**:

Add to `backend/KollectorScum.Api/appsettings.json`:

```json
{
  "Supabase": {
    "Url": "",
    "AnonKey": ""
  }
}
```

Add to `backend/KollectorScum.Api/appsettings.Development.json`:

```json
{
  "Supabase": {
    "Url": "http://localhost:54321",
    "AnonKey": "local-dev-key"
  }
}
```

*(Optional: For local dev, you can run Supabase locally via Docker, or skip storage in dev and use file system)*

### 7.4 Update Release Controller for Cloud Storage

**Modify the cover art upload logic** in `backend/KollectorScum.Api/Controllers/ReleasesController.cs` to be user-aware.

Before (file system):
```csharp
var filePath = Path.Combine(uploadsFolder, fileName);
using var stream = new FileStream(filePath, FileMode.Create);
await file.CopyToAsync(stream);
coverArtUrl = $"/cover-art/{fileName}";
```

After (Multi-Tenant Supabase Storage):
```csharp
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
if (string.IsNullOrEmpty(userId))
{
    return Unauthorized("User ID not found in token.");
}

using var stream = file.OpenReadStream();
coverArtUrl = await _storageService.UploadFileAsync("cover-art", userId, fileName, stream, file.ContentType);
```

**Add storage service to controller constructor**:

```csharp
private readonly IStorageService _storageService;

public ReleasesController(
    CollectionContext context,
    ILogger<ReleasesController> logger,
    IStorageService storageService)
{
    _context = context;
    _logger = logger;
    _storageService = storageService;
}
```

**Update delete endpoint to remove files from user's folder**:

In the `DELETE` endpoint, before deleting the release:

```csharp
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
if (string.IsNullOrEmpty(userId))
{
    return Unauthorized("User ID not found in token.");
}

// Delete cover art from storage if it exists
if (!string.IsNullOrEmpty(release.CoverArtUrl))
{
    var fileName = release.CoverArtUrl.Split('/').Last();
    await _storageService.DeleteFileAsync("cover-art", userId, fileName);
}
```

### 7.5 Configure Environment Variables

**Local development** (`backend/KollectorScum.Api/appsettings.Development.json`):
- Option 1: Use local file system (skip Supabase config)
- Option 2: Point to staging Supabase (share staging bucket during dev)

**Staging** (Render service environment variables):
```
Supabase__Url=https://YOUR-STAGING-PROJECT-REF.supabase.co
Supabase__AnonKey=YOUR-STAGING-ANON-KEY
```

**Production** (Render service environment variables):
```
Supabase__Url=https://YOUR-PROD-PROJECT-REF.supabase.co
Supabase__AnonKey=YOUR-PROD-ANON-KEY
```

### 7.6 Migration Strategy (Existing Cover Art)

With the move to multi-tenant storage, migrating existing cover art requires associating each image with its owner. The old migration strategies are no longer suitable.

**Recommended Approach: Programmatic Migration Endpoint**

The safest and most reliable method is to create a one-time, admin-only API endpoint that programmatically migrates the files. This ensures each image is uploaded to the correct user's folder in the cloud.

**Update the migration endpoint** (dev/staging only):

The logic must be updated to fetch the `UserId` for each release and pass it to the storage service.

```csharp
[HttpPost("migrate-storage")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> MigrateToCloudStorage()
{
    var releasesToMigrate = await _context.Releases
        .Where(r => !string.IsNullOrEmpty(r.CoverArtUrl) && r.CoverArtUrl.StartsWith("/cover-art/"))
        .ToListAsync();

    if (!releasesToMigrate.Any())
    {
        return Ok(new { Message = "No local images to migrate." });
    }

    var migratedCount = 0;
    foreach (var release in releasesToMigrate)
    {
        // The UserId is required to place the file in the correct user folder.
        if (release.UserId == Guid.Empty)
        {
            _logger.LogWarning("Skipping release {ReleaseId} due to missing UserId.", release.Id);
            continue;
        }

        var localPath = Path.Combine("wwwroot", release.CoverArtUrl.TrimStart('/'));
        
        if (System.IO.File.Exists(localPath))
        {
            try
            {
                using var stream = System.IO.File.OpenRead(localPath);
                var fileName = Path.GetFileName(localPath);
                
                // Call the multi-tenant upload method
                var newUrl = await _storageService.UploadFileAsync(
                    "cover-art", 
                    release.UserId.ToString(), 
                    fileName, 
                    stream, 
                    "image/jpeg" // Or determine dynamically
                );
                
                release.CoverArtUrl = newUrl;
                migratedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate cover art for release {ReleaseId}", release.Id);
            }
        }
    }

    await _context.SaveChangesAsync();
    return Ok(new { MigratedCount = migratedCount, TotalConsidered = releasesToMigrate.Count });
}
```

This updated endpoint correctly handles the multi-tenant requirements, making it the definitive method for migrating your existing images. After running this migration, all cover art will be served from the cloud, and the local `wwwroot/cover-art` directory will no longer be needed.

Note on validity: the guidance and checklist above (everything before **7.6.1**) remain valid — the migration endpoint approach and multi-tenant-storage recommendations still apply. The main operational change is that your staging environment will no longer use the local file system or Supabase storage for staging-only testing; instead, staging will use a Cloudflare R2 bucket (`cover-art-staging`) while production will use the chosen cloud provider (Supabase or R2, see below).

#### 7.6.1 Local multi-tenant migration (local-only)

If you want to move to a multi-tenant layout on the local filesystem first (safe and reversible), follow this checklist. Each task is a discrete step you can tick off as you complete it.

- [x] Add `IStorageService` interface (`backend/KollectorScum.Api/Services/IStorageService.cs`) with methods `UploadFileAsync`, `DeleteFileAsync`, and `GetPublicUrl`.
- [x] Implement `LocalFileSystemStorageService` (`backend/KollectorScum.Api/Services/LocalFileSystemStorageService.cs`) that stores files under `wwwroot/cover-art/{userId}/{filename}` and returns URLs like `/cover-art/{userId}/{filename}`.
- [x] Ensure `LocalFileSystemStorageService` sanitizes filenames via `Path.GetFileName` and restricts allowed extensions (jpg, jpeg, png, webp).
- [x] Register the local storage service in `Program.cs` (scoped `IStorageService`).
- [x] Update `ReleasesController` to use `IStorageService` for upload and delete operations, and to return per-user URLs.
- [x] Add an admin-only migration endpoint (example: `POST /releases/migrate-local-storage`) that:
    - [x] Finds releases where `CoverArtUrl` points to the old flat path (e.g. `/cover-art/{filename}`).
    - [x] Skips and logs releases missing `UserId` (do not guess owners).
    - [x] Copies files from `wwwroot/cover-art/{filename}` to `wwwroot/cover-art/{userId}/{filename}` using the storage service (copy first; do not delete originals yet).
    - [x] Verifies the copied file exists and is readable, then updates `release.CoverArtUrl` to `/cover-art/{userId}/{filename}`.
- [x] Backup `wwwroot/cover-art` before running the migration (e.g. `cp -a wwwroot/cover-art wwwroot/cover-art-backup-$(date +%Y%m%d)`).
- [x] Run the migration endpoint in `Development` and verify images load at `/cover-art/{userId}/{filename}` in the frontend. ✅ 2,359 images migrated successfully!
- [x] Add unit/integration tests for `LocalFileSystemStorageService` and the migration endpoint (verify path-safety, allowed extensions, and failure paths). ✅ 38 tests created covering path safety, extension validation, file size limits, and migration scenarios.
- [ ] (Optional) After verification, run a cleanup job to remove orphaned files not referenced by the DB and optionally delete the original flat files.

Notes: prefer copy-first behavior, validate content-types and sizes, and keep database backups before mass updates. This local-first approach makes later migration to cloud storage straightforward because every file will already be segmented by `userId`.

### 7.6.2 Staging: switch to Cloudflare R2 (`cover-art-staging`) ✅

Now that local storage is multi-tenant, switch the staging environment to use your Cloudflare R2 bucket `cover-art-staging`. This gives staging a production-like object store while keeping the repo and API changes minimal.

- [x] Create or reuse the Cloudflare R2 bucket `cover-art-staging`.
- [x] Provision an R2 access key (Access Key ID + Secret) scoped for the bucket.
- [x] Add the following environment variables to your Render staging service (or equivalent):

```
R2__AccountId=<your-account-id>
R2__Endpoint=https://<account_id>.r2.cloudflarestorage.com
R2__AccessKeyId=<R2_ACCESS_KEY_ID>
R2__SecretAccessKey=<R2_SECRET>
R2__BucketName=cover-art-staging
R2__PublicBaseUrl=https://<optional-worker-or-custom-domain>
```

- [x] Update `Program.cs` in staging to register the Cloudflare R2 storage service (or set an environment switch so the R2 implementation is used in staging). Example: `builder.Services.AddScoped<IStorageService, CloudflareR2StorageService>();`.
- [x] Deploy staging and run the migration endpoint in staging (admin-only) to migrate existing local `wwwroot/cover-art/{userId}/{filename}` items into the `cover-art-staging` bucket. Use the programmatic migration endpoint described in **7.6**; it will place objects under `{userId}/{filename}`.
- [x] Verify uploads, public URLs, and deletion behavior in the Cloudflare dashboard and via the staging frontend.

Notes: Using R2 for staging decouples your staging object store from Supabase limits and lets you test your R2-specific URL behavior (Workers/custom domains) before production.

### 7.6.3 Production: Cloudflare R2 rollout

With staging successfully running on Cloudflare R2, production follows the same pattern using the `cover-art-prod` bucket.

**Prerequisites:**
- Staging R2 implementation validated (upload, download, delete operations working)
- Production database migrations applied
- Production Supabase database ready and tested

**Production R2 setup:**

- [ ] **IMPORTANT: Complete database migration (Section 6.3) BEFORE setting up R2 storage.** This ensures UserIds match between staging and production, allowing you to copy R2 images directly.
- [ ] Create the Cloudflare R2 bucket `cover-art-prod` (or reuse if already created during staging setup).
- [ ] Provision production R2 access credentials (Access Key ID + Secret). **Use separate credentials from staging** for security isolation.
- [ ] Add the following environment variables to your Render production service:

```
R2__AccountId=<your-account-id>
R2__Endpoint=https://<account_id>.r2.cloudflarestorage.com
R2__AccessKeyId=<R2_PROD_ACCESS_KEY_ID>
R2__SecretAccessKey=<R2_PROD_SECRET>
R2__BucketName=cover-art-prod
R2__PublicBaseUrl=https://<production-worker-or-custom-domain>
```

- [ ] Update `Program.cs` production configuration to use `CloudflareR2StorageService` (should already be set if using environment-based registration from staging).
- [ ] Deploy to production (merge `dev` → `main` to trigger auto-deploy, or manually deploy from `main`).
- [ ] Verify production API health endpoint returns 200 OK.
- [ ] **Option A (Recommended)**: Copy staging R2 bucket contents to production R2 bucket. Since you migrated the database (which preserves UserIds), the R2 folder structure `{userId}/{filename}` will match between environments. Use Cloudflare R2 dashboard, rclone, or AWS CLI to copy objects from `cover-art-staging` to `cover-art-prod`.
- [ ] **Option B**: Run the migration endpoint in production (admin-only, **after DB and API backup**) to migrate existing cover art from local storage to `cover-art-prod`. This re-uploads images from the API server. Only needed if you didn't copy from staging R2.
- [ ] Verify a small subset of images load correctly before completing the full migration.
- [ ] Test the full upload/edit/delete cycle via the production frontend.
- [ ] Verify objects appear in the Cloudflare R2 dashboard under `cover-art-prod`.
- [ ] Set up Cloudflare Worker or custom domain for production image URLs (optional but recommended for friendly URLs and CDN caching).

**Pre-migration safety checklist:**
- [ ] Backup production database before migration.
- [ ] Backup `wwwroot/cover-art/` directory (if still present on production host).
- [ ] Test the migration endpoint on a **non-production copy** of the database first (or run in dry-run mode if implemented).
- [ ] Have a rollback plan (documented below).

**Post-migration verification:**
- [ ] Spot-check 10-20 random releases and verify cover art loads correctly.
- [ ] Monitor R2 usage and request metrics in Cloudflare dashboard.
- [ ] Check production logs for storage-related errors or warnings.
- [ ] Verify user-uploaded images (new uploads post-migration) work correctly.

**Rollback plan:**
If R2 has issues in production:
1. Revert the `Program.cs` registration to use `LocalFileSystemStorageService` (or a hotfix branch with filesystem code).
2. Redeploy API from `main` or a hotfix branch.
3. Restore database backup if cover art URLs were updated and need reverting.
4. Investigate R2 issues in staging before attempting production deployment again.

**Cost monitoring:**
- Cloudflare R2 offers 10GB storage free, then $0.015/GB/month.
- Class A operations (writes): 1M free/month, then $4.50 per million.
- Class B operations (reads): 10M free/month, then $0.36 per million.
- Egress: free (this is R2's main advantage over S3).

For <20 users, production should stay within free tier limits unless users upload thousands of high-res images monthly.

### 7.7 Update Dockerfile

Ensure the Dockerfile doesn't need to persist `wwwroot/cover-art/` as a volume:

**Before** (file system storage):
```dockerfile
# May need volume mounting for persistence
VOLUME ["/app/wwwroot/cover-art"]
```

**After** (cloud storage):
```dockerfile
# No volume needed; cover art is in Supabase
# Keep wwwroot for other static files if needed
```

### 7.8 Testing Checklist

**Local/Dev Testing**:
- [ ] Start API with Supabase config pointing to staging
- [ ] Upload a new release with cover art
- [ ] Verify image appears in Supabase dashboard → Storage → `cover-art` bucket
- [ ] Verify image is publicly accessible via returned URL
- [ ] Edit release and upload new cover art (verify old image is deleted)
- [ ] Delete release (verify cover art is deleted from bucket)

**Staging Testing**:
- [ ] Deploy API to Render staging
- [ ] Verify Supabase storage environment variables are set
- [ ] Test upload via staging frontend
- [ ] Verify images load in browser
- [ ] Check Supabase dashboard for uploaded files

**Production Testing** (after staging validation):
- [ ] Deploy to production
- [ ] Migrate existing cover art (if applicable)
- [ ] Test upload/delete operations
- [ ] Monitor storage usage in Supabase dashboard

### 7.9 Monitoring and Maintenance

**Storage limits (Supabase free tier)**:
- 1GB total storage
- Monitor usage: Supabase dashboard → Storage → Usage

**Image optimization recommendations**:
- Resize images on upload (max 800x800px for cover art)
- Convert to WebP format for better compression
- Validate file size before upload (< 500KB recommended)

**Cleanup strategy**:
- Implement orphaned file cleanup (files not referenced in DB)
- Schedule periodic bucket audits
- Consider TTL/expiration policies for temporary uploads

### 7.10 Rollback Plan

If Supabase Storage has issues:

1. **Revert backend code**:
   - Comment out `IStorageService` usage
   - Restore file system storage logic
   - Redeploy API

2. **Database URLs**:
   - Old releases keep Supabase URLs (will break)
   - New releases use local file system
   - Manual script to update URLs if needed

3. **Keep both systems running temporarily**:
   - Add fallback logic to check both locations
   - Gradually migrate back if needed

### 7.11 Optional Enhancements

**Image transformations**:
- Use Supabase image transformations (resize, crop, format conversion)
- Example: `{supabase-url}/storage/v1/render/image/public/cover-art/{filename}?width=400&height=400`

**Signed URLs for private content**:
- If cover art should be user-specific (multi-tenant isolation)
- Generate signed URLs with expiration
- Use service role key (keep secret) for URL signing

**CDN caching**:
- Supabase Storage includes CDN by default
- Set appropriate cache headers
- Consider CloudFlare in front for additional caching

### 7.12 Local / Console Actions (what you must run locally)

Follow these exact local/console steps when you implement the Cloudflare R2 storage plan so nothing is missed. Run the commands from the repository root unless otherwise noted.

- 1) Install the AWS S3 SDK for the backend (required to talk to R2):

```bash
cd backend/KollectorScum.Api
dotnet add package AWSSDK.S3
```

- 2) Create the storage service file (IStorageService implementation):
    - Create `backend/KollectorScum.Api/Services/CloudflareR2StorageService.cs` and paste the example implementation from Section 7.3.

- 3) Register the service in `Program.cs`:

```csharp
// Add Cloudflare R2 storage
builder.Services.AddScoped<IStorageService, CloudflareR2StorageService>();
```

- 4) Add required environment variables to your local `.env` (gitignored) and to your hosting provider (Render/GitHub Actions):

```
R2__AccountId=your-cloudflare-account-id
R2__Endpoint=https://<account_id>.r2.cloudflarestorage.com
R2__AccessKeyId=R2_ACCESS_KEY_ID
R2__SecretAccessKey=R2_SECRET
R2__BucketName=cover-art
R2__PublicBaseUrl=https://images.example.com   # optional Worker/custom domain
```

- 5) Provision Cloudflare R2 resources (Console):
    - Create R2 bucket(s) in Cloudflare: `cover-art-staging` and `cover-art-prod` (or single bucket with prefixes).
    - Generate an R2 Access Key (Access Key ID + Secret) and save them.
    - (Optional) Set up a Worker or custom domain for friendly image URLs.

- 6) Build and test the API locally with R2 configured:

```bash
cd backend/KollectorScum.Api
dotnet build
ASPNETCORE_ENVIRONMENT=Development R2__Endpoint="https://<account>.r2.cloudflarestorage.com" R2__AccessKeyId="..." R2__SecretAccessKey="..." dotnet run
```

- 7) Create and test the migration endpoint (dev/staging only):
    - Implement the `migrate-storage` admin endpoint described in Section 7.6 (use `IStorageService` implementation).
    - Run the endpoint locally or in staging to migrate existing local `wwwroot/cover-art` files to R2.

- 8) Image processing and backup (recommended before destructive passes):
    - Ensure you have a backup of originals (you indicated you have backups). If not, create one now:

```bash
cp -a /home/andy/music-images /home/andy/music-images-backup-$(date +%Y%m%d)
```

    - Install Node script deps (if not already):

```bash
npm install --prefix backend/scripts sharp minimist
```

    - Run simulation to estimate savings (non-destructive):

```bash
node backend/scripts/resize-cover-images.js --path /home/andy/music-images/covers --max 1600 --simulate
```

    - If satisfied, run the real pass (this overwrites originals in-place):

```bash
node backend/scripts/resize-cover-images.js --path /home/andy/music-images/covers --max 1600
```

- 9) Push code and deploy to staging:
    - Commit the new `.cs` file and `Program.cs` changes.
    - Push to `dev` to trigger staging CI/CD (Cloudflare Pages + Render staging).
    - In your hosting provider (Render/GitHub Actions), add `R2__*` env vars using the values from Cloudflare.

- 10) Staging verification checklist:
    - [ ] Upload a cover image via the staging frontend and verify it appears in R2.
    - [ ] Confirm the returned URL serves the image (via Worker/custom domain or R2 endpoint).
    - [ ] Edit/delete release and verify object deletion in R2.

- 11) Production rollout:
    - **Migrate database data** from staging to production (see Section 6.3 - one-time operation).
    - Run EF migrations for production (if needed).
    - Deploy the backend to production with production `R2__*` env vars set.
    - Run the storage migration endpoint in production (admin-only) to upload cover art images to R2 `cover-art-prod`.
    - Verify production database contains expected data and images load correctly.

- 12) Post-deploy monitoring and cleanup:
    - Monitor R2 usage in Cloudflare dashboard.
    - Implement orphan cleanup and lifecycle rules as needed.

These steps explicitly cover the local and console actions the plan assumes; follow them in order and ask for help at any step if you want me to create the files or run the commands here.

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
**Initial production setup (one-time):**
1. Push changes to `dev`
2. Verify staging works
3. Apply EF Core migrations to staging (if needed)
4. **Migrate database data from staging → production** (Section 6.3)
5. Merge `dev` → `main`
6. Apply EF Core migrations to production (if needed)
7. **Migrate cover art images to production R2** (Section 7.6.3)
8. Smoke test production

**Subsequent releases:**
1. Push changes to `dev`
2. Verify staging works
3. Apply EF Core migrations to staging (if schema changed)
4. Merge `dev` → `main`
5. Apply EF Core migrations to production (if schema changed)
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
