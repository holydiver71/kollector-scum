#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
API_DIR="$ROOT_DIR/backend/KollectorScum.Api"
ENV_FILE="$ROOT_DIR/.env"

usage() {
  cat >&2 <<'EOF'
Run the backend locally, pointing at local/staging/production DB and R2 storage.

Usage:
  backend/scripts/run-api-local.sh --local
  backend/scripts/run-api-local.sh --staging
  backend/scripts/run-api-local.sh --production

Options:
  --port <port>          Override listening port (default 5072)
  --environment <name>   ASPNETCORE_ENVIRONMENT (default Development)

Notes:
  - --staging uses .env: KOLLECTOR_STAGING_DB_URL + R2_STAGING__* variables
  - --production uses .env: KOLLECTOR_PROD_DB_URL + R2_PROD__* variables
  - Converts postgresql:// URLs into an Npgsql key/value connection string.
  - Does NOT print the connection string.
  - R2 credentials are automatically loaded and mapped to R2__* environment variables.
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
    
    # Load Discogs credentials
    if discogs_token="$(read_dotenv_value "Discogs__Token" "$ENV_FILE" 2>/dev/null || true)"; then
      if [[ -n "$discogs_token" ]]; then
        export Discogs__Token="$discogs_token"
      fi
    fi
    
    # Load Google OAuth credentials
    if google_client_id="$(read_dotenv_value "Google__ClientId" "$ENV_FILE" 2>/dev/null || true)"; then
      if [[ -n "$google_client_id" ]]; then
        export Google__ClientId="$google_client_id"
      fi
    fi
    if google_client_secret="$(read_dotenv_value "Google__ClientSecret" "$ENV_FILE" 2>/dev/null || true)"; then
      if [[ -n "$google_client_secret" ]]; then
        export Google__ClientSecret="$google_client_secret"
      fi
    fi
    
    # Load Cloudflare R2 credentials for staging. Try mode-specific keys first
    # (R2_STAGING__*), then fall back to generic R2__* keys in the .env file.
    for key in AccountId Endpoint AccessKeyId SecretAccessKey BucketName PublicBaseUrl; do
      r2_env_key="R2__${key}"
      dot_key="R2_STAGING__${key}"

      val="$(read_dotenv_value "$dot_key" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -z "$val" ]]; then
        val="$(read_dotenv_value "$r2_env_key" "$ENV_FILE" 2>/dev/null || true)"
      fi

      if [[ -n "$val" ]]; then
        export "${r2_env_key}=${val}"
      fi
    done
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
    
    # Load Discogs credentials
    if discogs_token="$(read_dotenv_value "Discogs__Token" "$ENV_FILE" 2>/dev/null || true)"; then
      if [[ -n "$discogs_token" ]]; then
        export Discogs__Token="$discogs_token"
      fi
    fi
    
    # Load Google OAuth credentials
    if google_client_id="$(read_dotenv_value "Google__ClientId" "$ENV_FILE" 2>/dev/null || true)"; then
      if [[ -n "$google_client_id" ]]; then
        export Google__ClientId="$google_client_id"
      fi
    fi
    if google_client_secret="$(read_dotenv_value "Google__ClientSecret" "$ENV_FILE" 2>/dev/null || true)"; then
      if [[ -n "$google_client_secret" ]]; then
        export Google__ClientSecret="$google_client_secret"
      fi
    fi
    
    # Load Cloudflare R2 credentials for production. Try mode-specific keys first
    # (R2_PROD__*), then fall back to generic R2__* keys in the .env file.
    for key in AccountId Endpoint AccessKeyId SecretAccessKey BucketName PublicBaseUrl; do
      r2_env_key="R2__${key}"
      dot_key="R2_PROD__${key}"

      val="$(read_dotenv_value "$dot_key" "$ENV_FILE" 2>/dev/null || true)"
      if [[ -z "$val" ]]; then
        val="$(read_dotenv_value "$r2_env_key" "$ENV_FILE" 2>/dev/null || true)"
      fi

      if [[ -n "$val" ]]; then
        export "${r2_env_key}=${val}"
      fi
    done
    ;;
  *)
    echo "Unknown mode: $MODE" >&2
    exit 1
    ;;
esac

echo "Starting API on $ASPNETCORE_URLS (env=$ASPNETCORE_ENVIRONMENT, mode=${MODE#--})"
exec dotnet run
