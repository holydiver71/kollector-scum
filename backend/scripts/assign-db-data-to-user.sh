#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ENV_FILE="$ROOT_DIR/.env"

usage() {
  cat >&2 <<'EOF'
Assign data in staging/production to a specific user.

Why this exists:
- Multi-tenant support scopes data by UserId.
- Data copied between environments may be owned by the seeded admin user OR the empty GUID.

Usage:
  backend/scripts/assign-db-data-to-user.sh --staging --email <email> --diagnose
  backend/scripts/assign-db-data-to-user.sh --production --email <email> --diagnose

  backend/scripts/assign-db-data-to-user.sh --staging --email <email> --yes [--from-user-id <uuid>]
  backend/scripts/assign-db-data-to-user.sh --production --email <email> --yes [--from-user-id <uuid>]

Notes:
  - Requires repo-root .env:
      KOLLECTOR_STAGING_DB_URL
      KOLLECTOR_PROD_DB_URL
  - The target user must already exist in the target DB (sign in once via Google first).
EOF
}

read_dotenv_value() {
  local key="$1"
  local file="$2"

  python3 - "$file" "$key" <<'PY'
import sys

path, key = sys.argv[1], sys.argv[2]

def strip_quotes(val: str) -> str:
  val = val.strip()
  if len(val) >= 2 and val[0] in ('"', "'") and val[-1] == val[0]:
    return val[1:-1]
  return val

try:
  with open(path, 'r', encoding='utf-8') as f:
    for line in f:
      line = line.strip()
      if not line or line.startswith('#'):
        continue
      if '=' not in line:
        continue
      k, v = line.split('=', 1)
      if k.strip() != key:
        continue
      print(strip_quotes(v))
      sys.exit(0)
except FileNotFoundError:
  pass

sys.exit(1)
PY
}

ensure_sslmode_require_for_supabase_url() {
  local url="$1"

  if [[ "$url" != postgresql://* && "$url" != postgres://* ]]; then
    echo "$url"
    return 0
  fi

  if [[ "$url" != *"supabase."* ]]; then
    echo "$url"
    return 0
  fi

  if [[ "$url" == *"sslmode="* ]]; then
    echo "$url"
    return 0
  fi

  if [[ "$url" == *"?"* ]]; then
    echo "${url}&sslmode=require"
  else
    echo "${url}?sslmode=require"
  fi
}

MODE=""
EMAIL=""
CONFIRMED="false"
DIAGNOSE_ONLY="false"
FROM_USER_ID=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --staging|--production|--prod)
      MODE="$1"
      shift
      ;;
    --email)
      EMAIL="${2:-}"
      shift 2
      ;;
    --from-user-id)
      FROM_USER_ID="${2:-}"
      shift 2
      ;;
    --diagnose)
      DIAGNOSE_ONLY="true"
      shift
      ;;
    --yes)
      CONFIRMED="true"
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if [[ -z "$MODE" || -z "$EMAIL" ]]; then
  usage
  exit 1
fi

if [[ "$DIAGNOSE_ONLY" != "true" && "$CONFIRMED" != "true" ]]; then
  echo "Refusing to run without either --diagnose or --yes." >&2
  usage
  exit 1
fi

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE. Create it from .env.example." >&2
  exit 1
fi

RAW_URL=""
DB_LABEL=""

case "$MODE" in
  --staging)
    DB_LABEL="staging"
    if ! RAW_URL="$(read_dotenv_value "KOLLECTOR_STAGING_DB_URL" "$ENV_FILE")"; then
      echo "Missing KOLLECTOR_STAGING_DB_URL in $ENV_FILE" >&2
      exit 1
    fi
    ;;
  --production|--prod)
    DB_LABEL="production"
    if ! RAW_URL="$(read_dotenv_value "KOLLECTOR_PROD_DB_URL" "$ENV_FILE")"; then
      echo "Missing KOLLECTOR_PROD_DB_URL in $ENV_FILE" >&2
      exit 1
    fi
    ;;
  *)
    echo "Unknown mode: $MODE" >&2
    exit 1
    ;;
esac

DB_URL_SSL="$(ensure_sslmode_require_for_supabase_url "$RAW_URL")"

# Seeded admin user from migration 20251213105441_AddMultiTenantSupport.cs
ADMIN_USER_ID="12337b39-c346-449c-b269-33b2e820d74f"
EMPTY_USER_ID="00000000-0000-0000-0000-000000000000"

TARGET_USER_ID="$(
  psql "$DB_URL_SSL" -v ON_ERROR_STOP=1 -v email="$EMAIL" -qAt <<'SQL'
SELECT "Id"::text
FROM "ApplicationUsers"
WHERE lower("Email") = lower(:'email')
LIMIT 1;
SQL
)"

if [[ -z "$TARGET_USER_ID" ]]; then
  echo "No ApplicationUsers row found for email in $DB_LABEL: $EMAIL" >&2
  echo "Sign in once via Google (creates the user), then re-run this script." >&2
  exit 1
fi

if [[ -z "$FROM_USER_ID" ]]; then
  FROM_USER_ID="$ADMIN_USER_ID"
fi

if [[ "$DIAGNOSE_ONLY" == "true" ]]; then
  echo "Diagnosing $DB_LABEL ownership for user: $EMAIL ($TARGET_USER_ID)"
  echo "Seeded admin user: $ADMIN_USER_ID"
  echo "Empty user GUID:  $EMPTY_USER_ID"
  echo "Configured source owner (--from-user-id): $FROM_USER_ID"

  psql "$DB_URL_SSL" -v ON_ERROR_STOP=1 -q <<SQL
DO \$\$
DECLARE
  r RECORD;
  total_count BIGINT;
  target_count BIGINT;
  source_count BIGINT;
BEGIN
  RAISE NOTICE 'Ownership summary across UserId-scoped tables:';
  FOR r IN (
    SELECT table_schema, table_name
    FROM information_schema.columns
    WHERE table_schema = 'public'
      AND column_name = 'UserId'
      AND udt_name = 'uuid'
    ORDER BY table_name
  )
  LOOP
    EXECUTE format('SELECT count(*) FROM %I.%I', r.table_schema, r.table_name) INTO total_count;
    EXECUTE format('SELECT count(*) FROM %I.%I WHERE "UserId" = %L::uuid', r.table_schema, r.table_name, '$TARGET_USER_ID') INTO target_count;
    EXECUTE format('SELECT count(*) FROM %I.%I WHERE "UserId" = %L::uuid', r.table_schema, r.table_name, '$FROM_USER_ID') INTO source_count;
    IF total_count > 0 OR target_count > 0 OR source_count > 0 THEN
      RAISE NOTICE '%.%: total=% target=% source=%', r.table_schema, r.table_name, total_count, target_count, source_count;
    END IF;
  END LOOP;

  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='MusicReleases') THEN
    RAISE NOTICE 'Top owners for MusicReleases:';
    FOR r IN (
      SELECT "UserId"::text AS user_id, count(*) AS c
      FROM "MusicReleases"
      GROUP BY "UserId"
      ORDER BY c DESC
      LIMIT 10
    )
    LOOP
      RAISE NOTICE '  % => % rows', r.user_id, r.c;
    END LOOP;
  END IF;
END \$\$;
SQL

  echo "Diagnosis complete. Common reassignment sources are:" 
  echo "  - $EMPTY_USER_ID (empty GUID)"
  echo "  - $ADMIN_USER_ID (seeded admin)"
  echo "Then re-run with:"
  echo "  backend/scripts/assign-db-data-to-user.sh $MODE --email $EMAIL --yes --from-user-id <owner-uuid>"
  exit 0
fi

echo "Reassigning $DB_LABEL data from ($FROM_USER_ID) -> user ($TARGET_USER_ID)"

psql "$DB_URL_SSL" -v ON_ERROR_STOP=1 -q <<SQL
DO \$\$
DECLARE
  r RECORD;
  updated_count BIGINT;
BEGIN
  FOR r IN (
    SELECT table_schema, table_name
    FROM information_schema.columns
    WHERE table_schema = 'public'
      AND column_name = 'UserId'
      AND udt_name = 'uuid'
    ORDER BY table_name
  )
  LOOP
    EXECUTE format(
      'UPDATE %I.%I SET "UserId" = %L::uuid WHERE "UserId" = %L::uuid',
      r.table_schema,
      r.table_name,
      '$TARGET_USER_ID',
      '$FROM_USER_ID'
    );
    GET DIAGNOSTICS updated_count = ROW_COUNT;
    RAISE NOTICE 'Updated %.%: % rows', r.table_schema, r.table_name, updated_count;
  END LOOP;
END \$\$;
SQL

echo "Done. Refresh the app; you should now see the data under $EMAIL in $DB_LABEL."
