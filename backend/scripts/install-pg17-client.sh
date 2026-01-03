#!/bin/bash
set -euo pipefail

echo "Installing PostgreSQL 17 client tools..."
echo ""
echo "This will:"
echo "  1. Add PostgreSQL APT repository"
echo "  2. Install postgresql-client-17"
echo ""
read -p "Continue? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
  echo "Cancelled."
  exit 0
fi

# Import PostgreSQL signing key
sudo apt install -y curl ca-certificates
sudo install -d /usr/share/postgresql-common/pgdg
sudo curl -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc --fail https://www.postgresql.org/media/keys/ACCC4CF8.asc

# Add PostgreSQL APT repository
# Linux Mint is based on Ubuntu, so we need to use Ubuntu's codename
UBUNTU_CODENAME=$(grep UBUNTU_CODENAME /etc/os-release | cut -d= -f2 | tr -d '"')
if [[ -z "$UBUNTU_CODENAME" ]]; then
  UBUNTU_CODENAME=$(lsb_release -cs)
fi
echo "Using codename: $UBUNTU_CODENAME"
echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt ${UBUNTU_CODENAME}-pgdg main" | sudo tee /etc/apt/sources.list.d/pgdg.list

# Update and install
sudo apt update
sudo apt install -y postgresql-client-17

echo ""
echo "✓ PostgreSQL 17 client installed"
pg_dump --version
psql --version
