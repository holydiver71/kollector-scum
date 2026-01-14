#!/bin/bash
set -euo pipefail

# Quick verification script for production database setup
# Run after completing section 6.3 to verify everything works

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ENV_FILE="$ROOT_DIR/.env"

echo "=== Production Database Verification ==="
echo ""

# Check .env exists
if [[ ! -f "$ENV_FILE" ]]; then
  echo "❌ Missing $ENV_FILE"
  echo "   Create it from .env.example and fill in production credentials"
  exit 1
fi

# Load production DB URL
if ! source <(grep KOLLECTOR_PROD_DB_URL "$ENV_FILE" 2>/dev/null); then
  echo "❌ Missing KOLLECTOR_PROD_DB_URL in $ENV_FILE"
  exit 1
fi

if [[ -z "${KOLLECTOR_PROD_DB_URL:-}" ]]; then
  echo "❌ KOLLECTOR_PROD_DB_URL is empty in $ENV_FILE"
  exit 1
fi

echo "✓ Found KOLLECTOR_PROD_DB_URL in .env"
echo ""

# Test connection
echo "Testing database connection..."
if ! psql "$KOLLECTOR_PROD_DB_URL" -c "SELECT version();" > /dev/null 2>&1; then
  echo "❌ Failed to connect to production database"
  echo "   Check your connection string and network connectivity"
  exit 1
fi
echo "✓ Database connection successful"
echo ""

# Check schema migrations
echo "Checking EF migrations..."
MIGRATION_COUNT=$(psql "$KOLLECTOR_PROD_DB_URL" -t -c "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";" 2>/dev/null | xargs)
if [[ "$MIGRATION_COUNT" -eq 0 ]]; then
  echo "⚠️  No migrations applied yet"
  echo "   Run: backend/scripts/apply-ef-migrations.sh --production"
else
  echo "✓ Found $MIGRATION_COUNT migrations applied"
fi
echo ""

# Check data
echo "Checking data..."

USER_COUNT=$(psql "$KOLLECTOR_PROD_DB_URL" -t -c "SELECT COUNT(*) FROM \"ApplicationUsers\";" 2>/dev/null | xargs || echo "0")
RELEASE_COUNT=$(psql "$KOLLECTOR_PROD_DB_URL" -t -c "SELECT COUNT(*) FROM \"MusicReleases\";" 2>/dev/null | xargs || echo "0")
ARTIST_COUNT=$(psql "$KOLLECTOR_PROD_DB_URL" -t -c "SELECT COUNT(*) FROM \"Artists\";" 2>/dev/null | xargs || echo "0")

echo "  Users:    $USER_COUNT"
echo "  Releases: $RELEASE_COUNT"
echo "  Artists:  $ARTIST_COUNT"
echo ""

if [[ "$USER_COUNT" -eq 0 ]]; then
  echo "⚠️  No users found - data migration may not be complete"
  echo "   Follow step 5-6 in section-6.3-production-setup.md"
else
  echo "✓ Data found in production database"
fi
echo ""

# Check R2 credentials
echo "Checking R2 credentials..."
R2_FOUND=0
for key in AccountId Endpoint AccessKeyId SecretAccessKey BucketName; do
  env_key="R2_PROD__${key}"
  if grep -q "^${env_key}=" "$ENV_FILE" 2>/dev/null; then
    val=$(grep "^${env_key}=" "$ENV_FILE" | cut -d'=' -f2- | sed 's/^["'\'']\(.*\)["'\'']$/\1/')
    if [[ -n "$val" ]]; then
      R2_FOUND=$((R2_FOUND + 1))
    fi
  fi
done

if [[ "$R2_FOUND" -eq 5 ]]; then
  echo "✓ All R2 production credentials found in .env"
else
  echo "⚠️  Missing some R2 production credentials ($R2_FOUND/5 found)"
  echo "   Add R2_PROD__* variables to .env"
fi
echo ""

# Summary
echo "=== Summary ==="
if [[ "$MIGRATION_COUNT" -gt 0 ]] && [[ "$USER_COUNT" -gt 0 ]] && [[ "$R2_FOUND" -eq 5 ]]; then
  echo "✅ Production backend is ready!"
  echo ""
  echo "Next steps:"
  echo "  1. Start local API: backend/scripts/run-api-local.sh --production"
  echo "  2. Test in browser: http://localhost:5072/health"
  echo "  3. Start frontend and sign in to verify data"
else
  echo "⚠️  Production backend setup incomplete"
  echo ""
  echo "Follow the checklist in documentation/section-6.3-production-setup.md"
fi
