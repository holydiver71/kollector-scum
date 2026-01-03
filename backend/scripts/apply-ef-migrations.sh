#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
API_PROJECT="$ROOT_DIR/backend/KollectorScum.Api/KollectorScum.Api.csproj"

# Applies EF Core migrations to the target database.
#
# Usage:
#   backend/scripts/apply-ef-migrations.sh --staging
#   backend/scripts/apply-ef-migrations.sh --production
#   backend/scripts/apply-ef-migrations.sh "<connection-string-or-postgresql-url>"
#
# Secrets file support:
# - If "$ROOT_DIR/.env" exists it will be parsed (not sourced) to avoid shell expansion issues.
# - Set one of these variables in .env:
#     KOLLECTOR_STAGING_DB_URL="postgresql://..."
#     KOLLECTOR_PROD_DB_URL="postgresql://..."

ENV_FILE="$ROOT_DIR/.env"

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
      k = k.strip()
      if k != key:
        continue
      v = strip_quotes(v)
      print(v)
      sys.exit(0)
except FileNotFoundError:
  pass

sys.exit(1)
PY
}

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 --staging | --production | \"<connection-string-or-postgresql-url>\"" >&2
  exit 1
fi

case "$1" in
  --staging)
    if [[ ! -f "$ENV_FILE" ]]; then
      echo "No .env found at $ENV_FILE. Create it from .env.example." >&2
      exit 1
    fi

    if ! CONN_STR_RAW="$(read_dotenv_value "KOLLECTOR_STAGING_DB_URL" "$ENV_FILE")"; then
      echo "KOLLECTOR_STAGING_DB_URL is not set in $ENV_FILE" >&2
      exit 1
    fi
    ;;
  --production|--prod)
    if [[ ! -f "$ENV_FILE" ]]; then
      echo "No .env found at $ENV_FILE. Create it from .env.example." >&2
      exit 1
    fi

    if ! CONN_STR_RAW="$(read_dotenv_value "KOLLECTOR_PROD_DB_URL" "$ENV_FILE")"; then
      echo "KOLLECTOR_PROD_DB_URL is not set in $ENV_FILE" >&2
      exit 1
    fi
    ;;
  *)
    CONN_STR_RAW="$1"
    ;;
esac

# Supabase commonly provides a URL-style connection string (postgresql://...)
# but Npgsql expects a key/value connection string. Convert when needed.
if [[ "$CONN_STR_RAW" == postgresql://* || "$CONN_STR_RAW" == postgres://* ]]; then
  CONN_STR="$(
    python3 - "$CONN_STR_RAW" <<'PY'
import sys
import socket
from urllib.parse import urlparse, unquote

raw = sys.argv[1]

if "#" in raw:
  sys.stderr.write(
    "WARNING: The connection URL contains '#'. In URLs, '#' starts a fragment and can truncate passwords. "
    "If your DB password includes '#', it must be URL-encoded as '%23' (best: copy/paste from Supabase UI).\n"
  )

def parse_postgres_url(url: str):
  """Parse a postgres URL without failing on unescaped '[' in userinfo.

  Python's urlparse/urlsplit rejects netlocs containing '[' (treats as IPv6 host marker),
  but passwords can legitimately contain '['. Supabase URLs are typically well-formed,
  but local secrets may be pasted without URL-encoding.
  """
  try:
    u = urlparse(url)
    if u.hostname:
      host = u.hostname
      port = u.port or 5432
      user = u.username or ""
      password = unquote(u.password or "")
      database = (u.path or "").lstrip("/") or "postgres"
      return host, port, user, password, database
  except ValueError:
    pass

  # Manual fallback: scheme://userinfo@host:port/db
  if "://" not in url:
    raise ValueError("Invalid postgres URL")

  scheme, rest = url.split("://", 1)
  if "/" in rest:
    netloc, path = rest.split("/", 1)
    database = path.split("?", 1)[0].split("#", 1)[0] or "postgres"
  else:
    netloc, database = rest, "postgres"

  if "@" in netloc:
    userinfo, hostport = netloc.rsplit("@", 1)
  else:
    userinfo, hostport = "", netloc

  if ":" in userinfo:
    user, password = userinfo.split(":", 1)
  else:
    user, password = userinfo, ""

  # Handle host:port (Supabase host is DNS, not IPv6)
  if hostport.startswith("[") and "]" in hostport:
    # Bracketed IPv6, keep it.
    host_end = hostport.index("]") + 1
    host = hostport[:host_end]
    port_part = hostport[host_end:]
    if port_part.startswith(":"):
      port = int(port_part[1:])
    else:
      port = 5432
  elif ":" in hostport:
    host, port_s = hostport.rsplit(":", 1)
    port = int(port_s) if port_s.isdigit() else 5432
  else:
    host, port = hostport, 5432

  return host, port, unquote(user), unquote(password), database


def kv_quote(value: str) -> str:
  # Use the standard .NET connection string quoting rules.
  # Double quotes escape by doubling them.
  value = value.replace('"', '""')
  return f'"{value}"'


host, port, user, password, database = parse_postgres_url(raw)

# Warn about likely double URL-encoding ("%25" is an encoded "%").
# This does not print secrets; it just flags a common copy/paste + re-encode mistake.
try:
  if '%25' in raw:
    sys.stderr.write(
      "WARNING: The connection URL contains '%25' (encoded '%'). This often means the password was URL-encoded twice. "
      "Paste a fresh connection string from Supabase (or run the encoder only once).\n"
    )
except Exception:
  pass

# Helpful diagnostics (never prints password).
sys.stderr.write(f"INFO: Target DB: host={host} port={port} db={database} user={user}\n")

# Supabase pooler ports:
# - Session mode: *.pooler.supabase.com:5432 (user typically postgres.<project-ref>)
# - Transaction mode: db.<ref>.supabase.co:6543 (user typically postgres)
if "pooler.supabase.com" in host and int(port) != 5432:
  sys.stderr.write(
    "WARNING: This looks like a Supabase Session pooler hostname ('*.pooler.supabase.com'), "
    "which normally uses port 5432. Copy/paste the exact 'Session' pooler connection string from the Supabase UI.\n"
  )

if "pooler.supabase.com" in host and user.strip().lower() == "postgres":
  sys.stderr.write(
    "WARNING: This looks like a Supabase pooler host, but the username is 'postgres'. "
    "Pooler connection strings often require a username like 'postgres.<project-ref>'. "
    "Copy/paste the exact Supabase connection string for your production project (including username), "
    "or use the direct db.<ref>.supabase.co host if your network supports IPv6.\n"
  )

# Some environments don't have IPv6 routing. Supabase hostnames often resolve to IPv6 first,
# resulting in "Network is unreachable". Prefer IPv4 when possible.
try:
  if host.endswith(".supabase.co"):
    infos = socket.getaddrinfo(host.strip("[]"), port, socket.AF_INET, socket.SOCK_STREAM)
    if infos:
      host = infos[0][4][0]
    else:
      sys.stderr.write(
        "WARNING: This Supabase DB hostname has no IPv4 (A) record. "
        "If your network cannot reach IPv6, use the Supabase 'Connection pooling' (session) hostname, "
        "or run migrations from an IPv6-capable network/CI runner.\n"
      )
except Exception:
  # If IPv4 resolution fails, keep the original host.
  pass

# Supabase requires TLS.
print(
  f"Host={host};Port={port};Database={database};Username={kv_quote(user)};Password={kv_quote(password)};Ssl Mode=Require;Trust Server Certificate=true"
)
PY
  )"
else
  CONN_STR="$CONN_STR_RAW"
fi

cd "$ROOT_DIR"

dotnet tool restore

export ConnectionStrings__DefaultConnection="$CONN_STR"

# Design-time factory handles configuration; no need to start the full host.
dotnet ef database update --project "$API_PROJECT"

echo "Migrations applied successfully."
