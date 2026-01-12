#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
API_DIR="$ROOT_DIR/backend/KollectorScum.Api"
ENV_FILE="$ROOT_DIR/.env"

usage() {
  cat >&2 <<'EOF'
Run the backend locally, pointing at local/staging/production DB.

Usage:
  backend/scripts/run-api-local.sh --local
  backend/scripts/run-api-local.sh --staging
  backend/scripts/run-api-local.sh --production

Options:
  --port <port>          Override listening port (default 5072)
  --environment <name>   ASPNETCORE_ENVIRONMENT (default Development)

Notes:
  - --staging uses .env: KOLLECTOR_STAGING_DB_URL
  - --production uses .env: KOLLECTOR_PROD_DB_URL
  - Converts postgresql:// URLs into an Npgsql key/value connection string.
  - Does NOT print the connection string.
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

postgres_url_to_npgsql() {
  local raw="$1"

  if [[ "$raw" != postgresql://* && "$raw" != postgres://* ]]; then
    echo "$raw"
    return 0
  fi

  python3 - "$raw" <<'PY'
import sys
from urllib.parse import urlparse, unquote

raw = sys.argv[1]

u = urlparse(raw)
if not u.hostname:
  raise SystemExit('Invalid postgres URL')

host = u.hostname
port = u.port or 5432
user = unquote(u.username or '')
password = unquote(u.password or '')
database = (u.path or '').lstrip('/') or 'postgres'

def q(val: str) -> str:
  # .NET connection string quoting: double quotes, escaped by doubling.
  return '"' + val.replace('"', '""') + '"'

print(
  f"Host={host};Port={port};Database={database};Username={q(user)};Password={q(password)};Ssl Mode=Require;Trust Server Certificate=true"
)
PY
}

MODE=""
PORT="5072"
ASPNET_ENV="Development"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --local|--staging|--production|--prod)
      MODE="$1"
      shift
      ;;
    --port)
      PORT="${2:-}"
      shift 2
      ;;
    --environment)
      ASPNET_ENV="${2:-}"
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

if [[ -z "$MODE" ]]; then
  usage
  exit 1
fi

cd "$API_DIR"

export ASPNETCORE_ENVIRONMENT="$ASPNET_ENV"
export ASPNETCORE_URLS="http://localhost:${PORT}"

case "$MODE" in
  --local)
    # Use appsettings.* default connection string
    export Database__Target="local"
    ;;
  --staging)
    if [[ ! -f "$ENV_FILE" ]]; then
      echo "Missing $ENV_FILE. Create it from .env.example." >&2
      exit 1
    fi
    if ! RAW_URL="$(read_dotenv_value "KOLLECTOR_STAGING_DB_URL" "$ENV_FILE")"; then
      echo "Missing KOLLECTOR_STAGING_DB_URL in $ENV_FILE" >&2
      exit 1
    fi
    export ConnectionStrings__DefaultConnection="$(postgres_url_to_npgsql "$RAW_URL")"
    export Database__Target="staging"
    # Optional: load Cloudflare R2 credentials from .env so staging can use R2 storage
    # Prefer already-exported env vars; fall back to values in $ENV_FILE if present
    if [[ -z "${R2__Endpoint:-}" ]]; then
      R2__Endpoint_VAL="$(read_dotenv_value "R2__Endpoint" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -n "$R2__Endpoint_VAL" ]]; then
        export R2__Endpoint="$R2__Endpoint_VAL"
      fi
    fi
    if [[ -z "${R2__AccessKeyId:-}" ]]; then
      R2__AccessKeyId_VAL="$(read_dotenv_value "R2__AccessKeyId" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -n "$R2__AccessKeyId_VAL" ]]; then
        export R2__AccessKeyId="$R2__AccessKeyId_VAL"
      fi
    fi
    if [[ -z "${R2__SecretAccessKey:-}" ]]; then
      R2__SecretAccessKey_VAL="$(read_dotenv_value "R2__SecretAccessKey" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -n "$R2__SecretAccessKey_VAL" ]]; then
        export R2__SecretAccessKey="$R2__SecretAccessKey_VAL"
      fi
    fi
    if [[ -z "${R2__BucketName:-}" ]]; then
      R2__BucketName_VAL="$(read_dotenv_value "R2__BucketName" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -n "$R2__BucketName_VAL" ]]; then
        export R2__BucketName="$R2__BucketName_VAL"
      fi
    fi
    if [[ -z "${R2__PublicBaseUrl:-}" ]]; then
      R2__PublicBaseUrl_VAL="$(read_dotenv_value "R2__PublicBaseUrl" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -n "$R2__PublicBaseUrl_VAL" ]]; then
        export R2__PublicBaseUrl="$R2__PublicBaseUrl_VAL"
      fi
    fi
    ;;
  --production|--prod)
    if [[ ! -f "$ENV_FILE" ]]; then
      echo "Missing $ENV_FILE. Create it from .env.example." >&2
      exit 1
    fi
    if ! RAW_URL="$(read_dotenv_value "KOLLECTOR_PROD_DB_URL" "$ENV_FILE")"; then
      echo "Missing KOLLECTOR_PROD_DB_URL in $ENV_FILE" >&2
      exit 1
    fi
    export ConnectionStrings__DefaultConnection="$(postgres_url_to_npgsql "$RAW_URL")"
    export Database__Target="production"
    # Optional: load Cloudflare R2 credentials from .env so production can use R2 storage
    if [[ -z "${R2__Endpoint:-}" ]]; then
      R2__Endpoint_VAL="$(read_dotenv_value "R2__Endpoint" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -n "$R2__Endpoint_VAL" ]]; then
        export R2__Endpoint="$R2__Endpoint_VAL"
      fi
    fi
    if [[ -z "${R2__AccessKeyId:-}" ]]; then
      R2__AccessKeyId_VAL="$(read_dotenv_value "R2__AccessKeyId" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -n "$R2__AccessKeyId_VAL" ]]; then
        export R2__AccessKeyId="$R2__AccessKeyId_VAL"
      fi
    fi
    if [[ -z "${R2__SecretAccessKey:-}" ]]; then
      R2__SecretAccessKey_VAL="$(read_dotenv_value "R2__SecretAccessKey" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -n "$R2__SecretAccessKey_VAL" ]]; then
        export R2__SecretAccessKey="$R2__SecretAccessKey_VAL"
      fi
    fi
    if [[ -z "${R2__BucketName:-}" ]]; then
      R2__BucketName_VAL="$(read_dotenv_value "R2__BucketName" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -n "$R2__BucketName_VAL" ]]; then
        export R2__BucketName="$R2__BucketName_VAL"
      fi
    fi
    if [[ -z "${R2__PublicBaseUrl:-}" ]]; then
      R2__PublicBaseUrl_VAL="$(read_dotenv_value "R2__PublicBaseUrl" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -n "$R2__PublicBaseUrl_VAL" ]]; then
        export R2__PublicBaseUrl="$R2__PublicBaseUrl_VAL"
      fi
    fi
    ;;
  *)
    echo "Unknown mode: $MODE" >&2
    exit 1
    ;;
esac

echo "Starting API on $ASPNETCORE_URLS (env=$ASPNETCORE_ENVIRONMENT, mode=${MODE#--})"
exec dotnet run
