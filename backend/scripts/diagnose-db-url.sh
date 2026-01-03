#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
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
      if k.strip() != key:
        continue
      print(strip_quotes(v))
      sys.exit(0)
except FileNotFoundError:
  pass

sys.exit(1)
PY
}

if [[ ! -f "$ENV_FILE" ]]; then
  echo "ERROR: Missing $ENV_FILE" >&2
  exit 1
fi

if ! RAW_URL="$(read_dotenv_value "KOLLECTOR_PROD_DB_URL" "$ENV_FILE")"; then
  echo "ERROR: KOLLECTOR_PROD_DB_URL not found in $ENV_FILE" >&2
  exit 1
fi

echo "Diagnosing KOLLECTOR_PROD_DB_URL (password masked)..."
echo ""

python3 - "$RAW_URL" <<'PY'
import sys
import re
from urllib.parse import urlparse, unquote

raw = sys.argv[1]

print(f"✓ URL length: {len(raw)} characters")
print(f"✓ Starts with: {raw[:20]}...")

# Check for common issues
issues = []
warnings = []

if raw.startswith(' ') or raw.endswith(' '):
    issues.append("URL has leading/trailing whitespace (will cause auth failure)")

if '\n' in raw or '\r' in raw:
    issues.append("URL contains newline characters (will cause auth failure)")

if '#' in raw:
    # Check if # is in the password part
    if '@' in raw and '#' in raw.split('@')[0]:
        issues.append("URL contains unencoded '#' before @host (password likely truncated). Encode as %23")

if '%25' in raw:
    warnings.append("URL contains '%25' (double-encoded %). May indicate password was encoded twice")

# Try to parse
try:
    u = urlparse(raw)
    
    print(f"✓ Scheme: {u.scheme}")
    print(f"✓ Host: {u.hostname}")
    print(f"✓ Port: {u.port or 5432}")
    print(f"✓ Database: {(u.path or '').lstrip('/') or 'postgres'}")
    print(f"✓ Username: {u.username}")
    
    if u.password:
        pwd = u.password
        print(f"✓ Password: {'*' * len(pwd)} ({len(pwd)} chars)")
        
        # Check password encoding
        if pwd != unquote(pwd):
            print(f"  ℹ Password appears URL-encoded (will be decoded automatically)")
        
        # Check for suspicious patterns
        if ' ' in pwd:
            issues.append("Decoded password contains spaces (unusual for Supabase)")
        
        # Check for unencoded special chars that should be encoded
        special_chars = ['#', '?', '&', '@', '/', '\\', ' ']
        unencoded = [c for c in special_chars if c in raw.split('@')[0].split(':')[-1] and c not in [':', '/']]
        if unencoded:
            issues.append(f"Password in URL contains unencoded special characters: {unencoded}. These should be URL-encoded")
    else:
        issues.append("No password found in URL")
    
    # Supabase-specific checks
    if 'pooler.supabase.com' in (u.hostname or ''):
        if u.port == 5432:
            print("✓ Supabase Session pooler detected (port 5432)")
            if u.username and not u.username.startswith('postgres.'):
                warnings.append(f"Username '{u.username}' doesn't match session pooler pattern 'postgres.<project-ref>'")
        elif u.port == 6543:
            warnings.append("Port 6543 with pooler.supabase.com host (unusual; transaction mode usually uses db.*.supabase.co)")
    
    if 'supabase.co' in (u.hostname or '') and u.port == 6543:
        print("✓ Supabase Transaction pooler detected (port 6543)")
    
except Exception as e:
    issues.append(f"Failed to parse URL: {e}")

print("")
if issues:
    print("❌ ISSUES FOUND:")
    for issue in issues:
        print(f"   • {issue}")
    print("")
    print("RECOMMENDED FIX:")
    print("   1. Go to Supabase Dashboard → Project Settings → Database")
    print("   2. Click 'Connection string' → Choose 'Session' mode")
    print("   3. Copy the ENTIRE connection string (starts with postgresql://)")
    print("   4. In .env, replace KOLLECTOR_PROD_DB_URL with the copied string")
    print("      (ensure no extra quotes, spaces, or newlines)")
    sys.exit(1)

if warnings:
    print("⚠ WARNINGS:")
    for warning in warnings:
        print(f"   • {warning}")
    print("")

print("✓ URL format appears valid")
print("")
print("If authentication still fails:")
print("   • Reset database password in Supabase Dashboard")
print("   • Get fresh connection string with new password")
print("   • Ensure .env file is saved after updating")
PY
