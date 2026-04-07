#!/usr/bin/env bash
set -euo pipefail

# Script to create a local appsettings.Development.json from the example
# Prompts for secrets and replaces placeholders. Keeps the file out of git.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXAMPLE="$SCRIPT_DIR/../KollectorScum.Api/appsettings.Development.json.example"
TARGET="$SCRIPT_DIR/../KollectorScum.Api/appsettings.Development.json"

if [ ! -f "$EXAMPLE" ]; then
  echo "Example file not found: $EXAMPLE"
  exit 1
fi

if [ -f "$TARGET" ]; then
  read -p "$TARGET already exists. Overwrite? [y/N]: " ok
  case "$ok" in
    [yY]|[yY][eE][sS]) rm -f "$TARGET" ;;
    *) echo "Aborting."; exit 0 ;;
  esac
fi

cp "$EXAMPLE" "$TARGET"

echo "Creating $TARGET from example. Leave blank to keep placeholder values."

prompt_secret() {
  local varname="$1" prompt="$2" secret
  read -rp "$prompt: " secret
  echo "$secret"
}

escape_for_sed() {
  # Escape / and & for sed replacement
  printf '%s' "$1" | sed -e 's/[\/&]/\\&/g'
}

DISCOGS_TOKEN=$(prompt_secret "DISCOGS" "Discogs token")
JWT_KEY=$(prompt_secret "JWT" "JWT key (>=32 chars)")
GOOGLE_CLIENT_ID=$(prompt_secret "GOOGLE" "Google Client ID")
BOOTSTRAP_SECRET=$(prompt_secret "BOOTSTRAP" "Bootstrap secret (dev)")

if [ -n "$DISCOGS_TOKEN" ]; then
  esc=$(escape_for_sed "$DISCOGS_TOKEN")
  sed -i "s/REPLACE_WITH_YOUR_DISCOGS_PERSONAL_ACCESS_TOKEN/$esc/g" "$TARGET"
fi

if [ -n "$JWT_KEY" ]; then
  esc=$(escape_for_sed "$JWT_KEY")
  sed -i "s/REPLACE_WITH_A_SECURE_RANDOM_KEY_AT_LEAST_32_CHARACTERS/$esc/g" "$TARGET"
fi

if [ -n "$GOOGLE_CLIENT_ID" ]; then
  esc=$(escape_for_sed "$GOOGLE_CLIENT_ID")
  sed -i "s/REPLACE_WITH_YOUR_GOOGLE_OAUTH_CLIENT_ID/$esc/g" "$TARGET"
fi

if [ -n "$BOOTSTRAP_SECRET" ]; then
  esc=$(escape_for_sed "$BOOTSTRAP_SECRET")
  sed -i "s/REPLACE_WITH_A_SECURE_BOOTSTRAP_SECRET/$esc/g" "$TARGET"
fi

# Restrict permissions
chmod 600 "$TARGET"

echo "Created $TARGET with restricted permissions (600)."
echo "Keep this file local. It's listed in .gitignore; do not commit it."
echo "Alternatively, use environment variables or 'dotnet user-secrets' for per-user secret storage."

exit 0
