#!/bin/bash
set -e

echo "Setting up PostgreSQL database for Kollector Scum..."

# 1. Check/Install PostgreSQL
if ! command -v psql &> /dev/null; then
    echo "PostgreSQL not found. Attempting to install via Homebrew..."
    if command -v brew &> /dev/null; then
        brew install postgresql
        echo "PostgreSQL installed."
    else
        echo "Error: Homebrew not found. Please install PostgreSQL manually."
        exit 1
    fi
else
    echo "PostgreSQL is already installed."
fi

# 2. Start PostgreSQL Service
echo "Ensuring PostgreSQL service is running..."
if command -v brew &> /dev/null; then
    brew services start postgresql || true
    # Wait a bit for it to start
    sleep 3
fi

# 3. Create User and Database
echo "Configuring database user and database..."

# Check if 'postgres' user exists, create if not
if ! psql postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname='postgres'" | grep -q 1; then
    echo "Creating user 'postgres'..."
    createuser -s postgres
else
    echo "User 'postgres' already exists."
fi

# Set password for 'postgres' user
echo "Setting password for 'postgres' user..."
psql postgres -c "ALTER USER postgres WITH PASSWORD 'postgres';"

# Check if database exists, create if not
if ! psql -U postgres -lqt | cut -d \| -f 1 | grep -qw kollectorscum; then
    echo "Creating database 'kollectorscum'..."
    createdb -U postgres kollectorscum
else
    echo "Database 'kollectorscum' already exists."
fi

# 4. Restore Dotnet Tools
echo "Restoring dotnet tools..."
dotnet tool restore

# 5. Run Migrations
echo "Running EF Core Migrations..."
cd backend/KollectorScum.Api
dotnet ef database update

echo "Database setup complete!"
echo "You can now run the API and use 'populate_data.sh' to seed the database."
