#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ENV_FILE="$ROOT_DIR/.env"
API_DEV_SETTINGS="$ROOT_DIR/backend/KollectorScum.Api/appsettings.Development.json"

usage() {
  cat >&2 <<'EOF'
Populate Supabase staging with data from your local/dev database.

WARNING: This will DELETE (truncate) all data in the staging database public schema
(except __EFMigrationsHistory) before importing.

Usage:
  backend/scripts/copy-dev-db-to-staging.sh --yes [--source <conn-or-url>] [--staging <url>]

Defaults:
  - Source DB connection is read from backend/KollectorScum.Api/appsettings.Development.json
    (ConnectionStrings:DefaultConnection)
  - Staging DB URL is read from .env (KOLLECTOR_STAGING_DB_URL)

Examples:
  backend/scripts/copy-dev-db-to-staging.sh --yes
  backend/scripts/copy-dev-db-to-staging.sh --yes --source "Host=localhost;Port=5432;Database=kollectorscum;Username=postgres;Password=postgres"
  backend/scripts/copy-dev-db-to-staging.sh --yes --staging "postgresql://postgres:***@*.pooler.supabase.com:6543/postgres"

Notes:
  - Requires pg_dump and psql.
  - Only copies DATA (not schema). Run EF migrations on staging first.
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

read_dev_default_connection() {
  python3 - "$API_DEV_SETTINGS" <<'PY'
import json
import sys

path = sys.argv[1]
with open(path, 'r', encoding='utf-8') as f:
  data = json.load(f)

conn = (data.get('ConnectionStrings') or {}).get('DefaultConnection')
if not conn:
  raise SystemExit('Could not find ConnectionStrings:DefaultConnection')
print(conn)
PY
}

STAGING_URL=""
SOURCE_CONN=""
CONFIRMED="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --yes)
      CONFIRMED="true"
      shift
      ;;
    --source)
      SOURCE_CONN="${2:-}"
      shift 2
      ;;
    --staging)
      STAGING_URL="${2:-}"
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
  echo "Refusing to run without --yes (this truncates staging data)." >&2
  usage
  exit 1
fi

require_cmd pg_dump
require_cmd psql

if [[ -z "$SOURCE_CONN" ]]; then
  if [[ ! -f "$API_DEV_SETTINGS" ]]; then
    echo "Missing $API_DEV_SETTINGS; provide --source instead." >&2
    exit 1
  fi
  SOURCE_CONN="$(read_dev_default_connection)"
fi

to_libpq_conn() {
  local raw="$1"

  # If it's already a URL, libpq tools can use it directly.
  if [[ "$raw" == postgresql://* || "$raw" == postgres://* ]]; then
    echo "$raw"
    return 0
  fi

  # Convert common Npgsql-style key/value connection strings (semicolon-separated)
  # into libpq keyword/value format.
  python3 - "$raw" <<'PY'
import sys

raw = sys.argv[1]

parts = [p.strip() for p in raw.split(';') if p.strip()]
kv = {}
for part in parts:
  if '=' not in part:
    continue
  k, v = part.split('=', 1)
  kv[k.strip().lower()] = v.strip().strip('"')

def pick(*keys, default=''):
  for k in keys:
    if k in kv and kv[k] != '':
      return kv[k]
  return default

host = pick('host', 'server', 'data source')
port = pick('port', default='5432')
dbname = pick('database', 'initial catalog', 'dbname')
user = pick('username', 'user id', 'user')
password = pick('password', 'pwd')

if not host or not dbname or not user:
  raise SystemExit('Unable to parse source connection string; provide a postgresql:// URL via --source.')

# libpq keyword/value connection string
print(f"host={host} port={port} dbname={dbname} user={user} password={password}")
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

if [[ -z "$STAGING_URL" ]]; then
  if [[ ! -f "$ENV_FILE" ]]; then
    echo "No .env found at $ENV_FILE; provide --staging instead." >&2
    exit 1
  fi

  if ! STAGING_URL="$(read_dotenv_value "KOLLECTOR_STAGING_DB_URL" "$ENV_FILE")"; then
    echo "KOLLECTOR_STAGING_DB_URL is not set in $ENV_FILE; provide --staging instead." >&2
    exit 1
  fi
fi

SOURCE_CONN_LIBPQ="$(to_libpq_conn "$SOURCE_CONN")"
STAGING_URL_SSL="$(ensure_sslmode_require_for_supabase_url "$STAGING_URL")"

TMP_DIR="$(mktemp -d)"
DUMP_FILE="$TMP_DIR/dev-data.sql"
FILTERED_DUMP_FILE="$TMP_DIR/dev-data.filtered.sql"

cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

echo "Exporting data from source DB (data-only)..."

# Data-only export. Exclude EF migrations history so staging remains consistent.
# Use plain SQL so restore is just a single psql invocation.
PGDUMP_OPTS=(
  --data-only
  --inserts
  --no-owner
  --no-privileges
  # Exclude EF migrations history so staging remains consistent.
  # Use multiple patterns to be resilient to schema qualification.
  --exclude-table=__EFMigrationsHistory
  --exclude-table=public.__EFMigrationsHistory
  --exclude-table-data=__EFMigrationsHistory
  --exclude-table-data=public.__EFMigrationsHistory
)

# Support either key/value connection string (e.g. Host=...) or URL (postgresql://...)
pg_dump "${PGDUMP_OPTS[@]}" "$SOURCE_CONN_LIBPQ" > "$DUMP_FILE"

# Safety net: ensure the EF migrations history table is not imported.
# (Some pg_dump builds/inputs may still emit it despite exclude patterns.)
python3 - "$DUMP_FILE" "$FILTERED_DUMP_FILE" <<'PY'
import sys

src, dst = sys.argv[1], sys.argv[2]
needle = "__EFMigrationsHistory"

with open(src, 'r', encoding='utf-8', errors='replace') as fin, open(dst, 'w', encoding='utf-8') as fout:
  for line in fin:
    if needle in line:
      continue
    fout.write(line)
PY

echo "Clearing staging data (truncate public schema tables)..."

# Truncate everything in public schema except EF migrations history.
# RESTART IDENTITY resets sequences; CASCADE handles FKs.
psql "$STAGING_URL_SSL" -v ON_ERROR_STOP=1 -q <<'SQL'
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

echo "Importing data into staging..."
psql "$STAGING_URL_SSL" -v ON_ERROR_STOP=1 -q -f "$FILTERED_DUMP_FILE"

echo "Done. Staging has been populated from dev/local data."
