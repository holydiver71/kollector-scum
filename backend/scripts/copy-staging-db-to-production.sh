#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ENV_FILE="$ROOT_DIR/.env"

usage() {
  cat >&2 <<'EOF'
Copy ALL data (data-only) from Supabase staging into Supabase production.

DANGER:
- This will TRUNCATE ALL data in the production database public schema
  (except __EFMigrationsHistory) before importing.
- This is intended for initial production bootstrap only.

Usage:
  backend/scripts/copy-staging-db-to-production.sh --yes-i-understand-this-will-delete-production-data

Optional overrides:
  backend/scripts/copy-staging-db-to-production.sh --yes-i-understand-this-will-delete-production-data \
    --staging "<postgresql://...>" --production "<postgresql://...>"

Defaults:
  - Staging DB URL from repo-root .env: KOLLECTOR_STAGING_DB_URL
  - Production DB URL from repo-root .env: KOLLECTOR_PROD_DB_URL

Notes / lessons learned from staging:
- Run EF migrations on production first (schema must exist).
- Excludes __EFMigrationsHistory from the dump and strips it as a safety net.
- Supabase requires TLS; this script enforces sslmode=require if missing.
EOF
}

require_cmd() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "Missing required command: $cmd" >&2
    exit 1
  fi
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

CONFIRMED="false"
STAGING_URL=""
PROD_URL=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --yes-i-understand-this-will-delete-production-data)
      CONFIRMED="true"
      shift
      ;;
    --staging)
      STAGING_URL="${2:-}"
      shift 2
      ;;
    --production|--prod)
      PROD_URL="${2:-}"
      shift 2
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

if [[ "$CONFIRMED" != "true" ]]; then
  echo "Refusing to run without --yes-i-understand-this-will-delete-production-data" >&2
  usage
  exit 1
fi

require_cmd pg_dump
require_cmd psql

if [[ -z "$STAGING_URL" || -z "$PROD_URL" ]]; then
  if [[ ! -f "$ENV_FILE" ]]; then
    echo "Missing $ENV_FILE. Create it from .env.example." >&2
    exit 1
  fi
fi

if [[ -z "$STAGING_URL" ]]; then
  if ! STAGING_URL="$(read_dotenv_value "KOLLECTOR_STAGING_DB_URL" "$ENV_FILE")"; then
    echo "Missing KOLLECTOR_STAGING_DB_URL in $ENV_FILE" >&2
    exit 1
  fi
fi

if [[ -z "$PROD_URL" ]]; then
  if ! PROD_URL="$(read_dotenv_value "KOLLECTOR_PROD_DB_URL" "$ENV_FILE")"; then
    echo "Missing KOLLECTOR_PROD_DB_URL in $ENV_FILE" >&2
    exit 1
  fi
fi

STAGING_URL_SSL="$(ensure_sslmode_require_for_supabase_url "$STAGING_URL")"
PROD_URL_SSL="$(ensure_sslmode_require_for_supabase_url "$PROD_URL")"

TMP_DIR="$(mktemp -d)"
DUMP_FILE="$TMP_DIR/staging-data.sql"
FILTERED_DUMP_FILE="$TMP_DIR/staging-data.filtered.sql"

cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

echo "Exporting data from staging (data-only)..."

PGDUMP_OPTS=(
  --data-only
  --inserts
  --no-owner
  --no-privileges
  --schema=public
  --exclude-table=__EFMigrationsHistory
  --exclude-table=public.__EFMigrationsHistory
  --exclude-table-data=__EFMigrationsHistory
  --exclude-table-data=public.__EFMigrationsHistory
  --exclude-table=schema_migrations
  --exclude-table=public.schema_migrations
  --exclude-table-data=schema_migrations
  --exclude-table-data=public.schema_migrations
  --exclude-table=__reset_marker
  --exclude-table=public.__reset_marker
  --exclude-table-data=__reset_marker
  --exclude-table-data=public.__reset_marker
)

pg_dump "${PGDUMP_OPTS[@]}" "$STAGING_URL_SSL" > "$DUMP_FILE"

# Safety net: ensure the EF migrations history table and schema_migrations are not imported.
python3 - "$DUMP_FILE" "$FILTERED_DUMP_FILE" <<'PY'
import sys

src, dst = sys.argv[1], sys.argv[2]
exclude_patterns = ["__EFMigrationsHistory", "schema_migrations"]

with open(src, 'r', encoding='utf-8', errors='replace') as fin, open(dst, 'w', encoding='utf-8') as fout:
  for line in fin:
    if any(pattern in line for pattern in exclude_patterns):
      continue
    fout.write(line)
PY

echo "Clearing production data (truncate public schema tables)..."

psql "$PROD_URL_SSL" -v ON_ERROR_STOP=1 -q <<'SQL'
DO $$
DECLARE
  r RECORD;
BEGIN
  FOR r IN (
    SELECT tablename
    FROM pg_tables
    WHERE schemaname = 'public'
      AND tablename <> '__EFMigrationsHistory'
  )
  LOOP
    EXECUTE 'TRUNCATE TABLE public.' || quote_ident(r.tablename) || ' RESTART IDENTITY CASCADE';
  END LOOP;
END $$;
SQL

echo "Importing data into production..."
psql "$PROD_URL_SSL" -v ON_ERROR_STOP=1 -q -f "$FILTERED_DUMP_FILE"

echo "Done. Production has been populated from staging data."
