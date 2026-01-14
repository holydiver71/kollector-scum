# Section 6.3: Production Database Migration Guide

This guide walks through migrating your staging database and R2 storage to production.

## Prerequisites

- [ ] Staging environment is running and stable
- [ ] You have a Supabase account
- [ ] You have a Cloudflare account with R2 enabled

## Step 1: Create Production Supabase Project

1. Go to [Supabase Dashboard](https://supabase.com/dashboard)
2. Click "New Project"
3. Fill in:
   - **Name**: `kollector-scum-prod`
   - **Database Password**: Generate a strong password (save it!)
   - **Region**: Choose same region as staging for consistency
4. Wait for project to provision (~2 minutes)

## Step 2: Get Production Connection String

1. In your new production project, go to **Project Settings → Database**
2. Find the **Connection string** section
3. Click the **URI** tab
4. Copy the connection string (it will look like):
   ```
   postgresql://postgres:[YOUR-PASSWORD]@db.xxxxxxxxxxxxx.supabase.co:5432/postgres
   ```
5. Save this - you'll need it in the next step

## Step 3: Update Your `.env` File

1. Open `/home/andy/Projects/kollector-scum/.env` (create from `.env.example` if it doesn't exist)
2. Add/update the production database URL:
   ```bash
   KOLLECTOR_PROD_DB_URL="postgresql://postgres:[YOUR-PASSWORD]@db.xxxxxxxxxxxxx.supabase.co:5432/postgres"
   ```

## Step 4: Apply EF Core Migrations to Production

Run the migration script to create the schema in production:

```bash
cd /home/andy/Projects/kollector-scum
backend/scripts/apply-ef-migrations.sh --production
```

**Expected output:**
```
Applying EF Core migrations to production...
✓ Migrations applied successfully
```

If you see errors about IPv6, use the pooler connection string instead (see `.env.example` for format).

## Step 5: Export Data from Staging

Create a data-only dump from staging (excludes schema and migration history):

```bash
cd /home/andy/Projects/kollector-scum

# Load staging URL from .env
source <(grep KOLLECTOR_STAGING_DB_URL .env)

# Create the dump
pg_dump "$KOLLECTOR_STAGING_DB_URL" \
  --data-only \
  --no-owner \
  --no-privileges \
  --exclude-table-data=__EFMigrationsHistory \
  --file=staging_data_backup_$(date +%Y%m%d_%H%M%S).sql

# Verify the file was created
ls -lh staging_data_backup_*.sql
```

**What this does:**
- Exports all data from staging (ApplicationUsers, Releases, etc.)
- Excludes schema (we already applied that with EF migrations)
- Excludes `__EFMigrationsHistory` table (specific to each environment)
- Saves with timestamp so you can keep multiple backups

## Step 6: Import Data to Production

**⚠️ IMPORTANT: Double-check you're using the PROD URL, not staging!**

```bash
# Load production URL from .env
source <(grep KOLLECTOR_PROD_DB_URL .env)

# Import the data (use the most recent backup file)
psql "$KOLLECTOR_PROD_DB_URL" -f staging_data_backup_<timestamp>.sql

# Check for errors in the output
# Common errors:
#   - Duplicate key violations (re-running import) - safe to ignore if expected
#   - Foreign key violations - indicates data integrity issue, investigate
```

## Step 7: Verify Production Database

```bash
# Connect to production and verify data
psql "$KOLLECTOR_PROD_DB_URL" -c "SELECT COUNT(*) AS user_count FROM \"ApplicationUsers\";"
psql "$KOLLECTOR_PROD_DB_URL" -c "SELECT COUNT(*) AS release_count FROM \"Releases\";"
psql "$KOLLECTOR_PROD_DB_URL" -c "SELECT COUNT(*) AS artist_count FROM \"Artists\";"

# Verify your user exists
psql "$KOLLECTOR_PROD_DB_URL" -c "SELECT \"Id\", \"Email\", \"DisplayName\", \"IsAdmin\" FROM \"ApplicationUsers\" WHERE \"Email\" = 'andy.shutt@gmail.com';"
```

**Expected:**
- User count should match staging
- Release count should match staging
- Your andy.shutt@gmail.com user should exist with `IsAdmin = true`

## Step 8: Set Up Production R2 Credentials

1. Go to [Cloudflare Dashboard](https://dash.cloudflare.com/)
2. Navigate to **R2 Object Storage**
3. Create bucket: `cover-art-prod` (or it might already exist)
4. Go to **Manage R2 API Tokens**
5. Create a new token:
   - **Token Name**: `kollector-scum-prod`
   - **Permissions**: Object Read & Write
   - **Buckets**: `cover-art-prod` (or "Apply to all buckets")
6. Copy the credentials shown (you can only see them once!):
   - Access Key ID
   - Secret Access Key
   - Endpoint URL (should be `https://<account_id>.r2.cloudflarestorage.com`)

7. Update your `.env` file:
   ```bash
   # Cloudflare R2 (S3-compatible) - production
   R2_PROD__AccountId=<your-account-id>
   R2_PROD__Endpoint=https://<account_id>.r2.cloudflarestorage.com
   R2_PROD__AccessKeyId=<access-key-id>
   R2_PROD__SecretAccessKey=<secret-access-key>
   R2_PROD__BucketName=cover-art-prod
   R2_PROD__PublicBaseUrl=https://<your-worker-or-custom-domain>
   ```

## Step 9: Test Local API with Production Backend

Start the API locally connected to production:

```bash
cd /home/andy/Projects/kollector-scum
backend/scripts/run-api-local.sh --production
```

**Expected output:**
```
Starting API on http://localhost:5072 (env=Development, mode=production)
...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5072
```

**Verify:**
1. API starts without errors
2. In another terminal, test the health endpoint:
   ```bash
   curl http://localhost:5072/health
   ```
   Should return `200 OK`

3. Test authentication (optional - requires frontend running):
   - Start frontend: `cd frontend && npm run dev`
   - Open http://localhost:3000
   - Sign in with your Google account
   - Verify you see your releases from staging

## Step 10: Copy R2 Images from Staging to Production

Since you migrated the database (preserving UserIds), you can copy R2 images directly:

**Option A: Using AWS CLI (if installed)**

```bash
# Configure AWS CLI for Cloudflare R2
aws configure set aws_access_key_id <staging-access-key> --profile r2-staging
aws configure set aws_secret_access_key <staging-secret> --profile r2-staging

aws configure set aws_access_key_id <prod-access-key> --profile r2-prod
aws configure set aws_secret_access_key <prod-secret> --profile r2-prod

# Copy from staging to production
aws s3 sync \
  s3://cover-art-staging \
  s3://cover-art-prod \
  --endpoint-url https://<account-id>.r2.cloudflarestorage.com \
  --profile r2-staging \
  --source-region auto \
  --region auto
```

**Option B: Using Cloudflare Dashboard**

1. Use [rclone](https://rclone.org/) (recommended) or manually download/upload via dashboard
2. This is slower but doesn't require AWS CLI setup

**Option C: Re-run migration endpoint in production**

If R2 copy doesn't work, you can re-migrate from local storage:

```bash
# Ensure API is running with --production
# Then call the migration endpoint (requires admin auth token)
curl -X POST http://localhost:5072/api/releases/migrate-storage \
  -H "Authorization: Bearer <your-jwt-token>"
```

## Step 11: Verify Images Load

1. With the local API running (`--production`), open your local frontend
2. Navigate to a release with cover art
3. Verify the image loads
4. Check the image URL in browser dev tools - should point to production R2 bucket

## Step 12: Update Frontend Environment Variables

Update your frontend to point to the production API when needed:

```bash
# frontend/.env.local (for testing against production)
NEXT_PUBLIC_API_BASE_URL=http://localhost:5072
NEXT_PUBLIC_GOOGLE_CLIENT_ID=<your-google-client-id>
```

## Verification Checklist

- [ ] Production Supabase project created
- [ ] Production database schema applied (EF migrations)
- [ ] Data exported from staging
- [ ] Data imported to production
- [ ] Production database verified (user count, release count match)
- [ ] Production R2 bucket created
- [ ] Production R2 credentials generated and saved in `.env`
- [ ] Local API connects to production successfully
- [ ] Images copied from staging R2 to production R2
- [ ] Images load correctly through local API

## Rollback Plan

If something goes wrong:

1. **Database issues:**
   - Delete the production Supabase project
   - Recreate and start over
   - Your staging data is unchanged

2. **R2 issues:**
   - Delete objects in production R2 bucket
   - Start over with copy/migration

3. **Connection issues:**
   - Verify `.env` has correct credentials
   - Try pooler connection string if IPv6 is the issue
   - Check Supabase project status in dashboard

## Next Steps

Once production backend is verified locally:
- Section 7.6.3: Deploy API to Render production
- Section 9: Deploy frontend to Cloudflare Pages production
- Section 12: Complete production release checklist

## Troubleshooting

**"Network is unreachable" during migration:**
- Your ISP may not support IPv6
- Use the pooler connection string from Supabase instead

**"password authentication failed":**
- Verify you copied the connection string correctly from Supabase
- Check for extra spaces or quotes in `.env`

**"duplicate key value violates unique constraint":**
- You've already imported this data
- Safe to ignore if you're re-running the import
- To start fresh, truncate all tables in production first

**Images don't load:**
- Verify R2 credentials are correct in `.env`
- Check the bucket name matches (`cover-art-prod`)
- Verify `R2_PROD__*` variables are set (not just `R2_STAGING__*`)
- Check browser console for CORS errors
