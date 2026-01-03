#!/bin/bash
set -euo pipefail

# URL-encodes the username/password portions of KOLLECTOR_*_DB_URL entries in the repo-root .env file.
#
# Why: Supabase provides URLs like postgresql://postgres:<password>@db.<ref>.supabase.co:5432/postgres
# If the password contains reserved URL characters, it must be percent-encoded.
#
# Usage:
#   backend/scripts/encode-db-urls-env.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ENV_FILE="$ROOT_DIR/.env"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "No .env found at $ENV_FILE" >&2
  exit 1
fi

python3 - "$ENV_FILE" <<'PY'
import sys
import re
from urllib.parse import urlparse, quote, unquote

path = sys.argv[1]

def encode_url(url: str) -> str:
    url = url.strip()
    if not (url.startswith("postgresql://") or url.startswith("postgres://")):
        return url

    def manual_parse(url: str):
        scheme, rest = url.split("://", 1)
        if "/" in rest:
            netloc, path = rest.split("/", 1)
            path_part = "/" + path
        else:
            netloc, path_part = rest, "/"

        if "@" in netloc:
            userinfo, hostport = netloc.rsplit("@", 1)
        else:
            userinfo, hostport = "", netloc

        if ":" in userinfo:
            user, password = userinfo.split(":", 1)
        else:
            user, password = userinfo, ""

        return scheme, user, password, hostport, path_part

    try:
        u = urlparse(url)
        if not u.hostname:
            raise ValueError("no hostname")

        scheme = u.scheme
        username = u.username or ""
        password = unquote(u.password or "")
        host = u.hostname
        hostport = host
        if u.port:
            hostport = f"{host}:{u.port}"
        path_part = u.path or "/"
        if not path_part.startswith("/"):
            path_part = "/" + path_part
        query = u.query
        fragment = u.fragment
    except ValueError:
        scheme, username, password, hostport, path_part = manual_parse(url)
        query = ""
        fragment = ""

    enc_user = quote(unquote(username), safe="")
    enc_pass = quote(unquote(password), safe="")

    netloc = hostport
    if username or password:
        netloc = f"{enc_user}:{enc_pass}@{hostport}"

    rebuilt = f"{scheme}://{netloc}{path_part}"
    if query:
        rebuilt += f"?{query}"
    if fragment:
        rebuilt += f"#{fragment}"
    return rebuilt

key_re = re.compile(r'^(KOLLECTOR_(?:STAGING|PROD)_DB_URL)=(.*)$')

lines = []
with open(path, 'r', encoding='utf-8') as f:
    for line in f:
        m = key_re.match(line.rstrip('\n'))
        if not m:
            lines.append(line)
            continue

        key, raw_value = m.group(1), m.group(2).strip()

        # Preserve quoting style
        quote_char = ""
        value = raw_value
        if len(value) >= 2 and value[0] in ('"', "'") and value[-1] == value[0]:
            quote_char = value[0]
            value = value[1:-1]

        encoded = encode_url(value)
        if quote_char:
            encoded = f"{quote_char}{encoded}{quote_char}"

        lines.append(f"{key}={encoded}\n")

with open(path, 'w', encoding='utf-8') as f:
    f.writelines(lines)
PY

echo "Updated .env: URL-encoded KOLLECTOR_STAGING_DB_URL / KOLLECTOR_PROD_DB_URL (if applicable)."
