#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ENV_FILE="$ROOT_DIR/.env"

usage() {
  cat >&2 <<'EOF'
Test DB connectivity for staging/production without printing secrets.

Usage:
  backend/scripts/test-db-connection.sh --staging
  backend/scripts/test-db-connection.sh --production

Reads repo-root .env:
  - KOLLECTOR_STAGING_DB_URL
  - KOLLECTOR_PROD_DB_URL

Notes:
  - Requires `psql`.
  - Does not echo the connection string.
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

MODE="${1:-}"
case "$MODE" in
  --staging|--production|--prod) ;;
  -h|--help|"")
    usage
    exit 0
    ;;
  *)
    echo "Unknown argument: $MODE" >&2
    usage
    exit 1
    ;;
esac

require_cmd psql

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE. Create it from .env.example." >&2
  exit 1
fi

KEY=""
LABEL=""
if [[ "$MODE" == "--staging" ]]; then
  KEY="KOLLECTOR_STAGING_DB_URL"
  LABEL="staging"
else
  KEY="KOLLECTOR_PROD_DB_URL"
  LABEL="production"
fi

if ! RAW_URL="$(read_dotenv_value "$KEY" "$ENV_FILE")"; then
  echo "Missing $KEY in $ENV_FILE" >&2
  exit 1
fi

URL_SSL="$(ensure_sslmode_require_for_supabase_url "$RAW_URL")"

# Print safe diagnostics (never prints password).
python3 - <<'PY' "$RAW_URL"
import sys
from urllib.parse import urlparse

raw = sys.argv[1]
u = urlparse(raw)

if "#" in raw:
  print(
    "WARNING: The connection URL contains '#'. In URLs, '#' starts a fragment and can truncate passwords. "
    "If your DB password includes '#', it must be URL-encoded as '%23' (best: copy/paste from Supabase UI)."
  )

host = u.hostname or ""
port = u.port or 5432
user = u.username or ""
database = (u.path or "").lstrip("/") or "postgres"

print(f"INFO: Target DB: host={host} port={port} db={database} user={user}")

if "pooler.supabase.com" in host and port == 5432:
  # Supabase session pooler commonly uses a routing username like postgres.<project-ref>
  if user.strip().lower() == "postgres":
    print(
      "WARNING: This looks like a Supabase Session pooler host ('*.pooler.supabase.com:5432'), "
      "but the username is just 'postgres'. Copy/paste the exact 'Session' pooler connection string from Supabase."
    )
  elif user.startswith("postgres."):
    print(
      "NOTE: Supabase Session pooler usernames often look like 'postgres.<project-ref>'. "
      "Server auth errors may still report user 'postgres' after routing."
    )
PY

echo "Testing $LABEL database connectivity..."

# Keep output minimal and secret-free.
psql "$URL_SSL" -v ON_ERROR_STOP=1 -qAt <<'SQL' >/dev/null
SELECT 1;
SQL

echo "OK: authenticated and executed query against $LABEL."
