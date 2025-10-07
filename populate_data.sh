#!/bin/bash

# Simple script to populate database via HTTP endpoints

BASE_URL="http://localhost:5072"

echo "Starting data population..."

# Seed lookup tables first (order matters due to relationships)
echo "Seeding Countries..."
curl -X POST "$BASE_URL/api/seed/countries" -H "Content-Type: application/json"
echo ""

echo "Seeding Stores..."
curl -X POST "$BASE_URL/api/seed/stores" -H "Content-Type: application/json"
echo ""

echo "Seeding Formats..."
curl -X POST "$BASE_URL/api/seed/formats" -H "Content-Type: application/json"
echo ""

echo "Seeding Genres..."
curl -X POST "$BASE_URL/api/seed/genres" -H "Content-Type: application/json"
echo ""

echo "Seeding Labels..."
curl -X POST "$BASE_URL/api/seed/labels" -H "Content-Type: application/json"
echo ""

echo "Seeding Artists..."
curl -X POST "$BASE_URL/api/seed/artists" -H "Content-Type: application/json"
echo ""

echo "Seeding Packagings..."
curl -X POST "$BASE_URL/api/seed/packagings" -H "Content-Type: application/json"
echo ""

# Seed all lookup tables at once
echo "Seeding all lookup tables..."
curl -X POST "$BASE_URL/api/seed/lookup-data" -H "Content-Type: application/json"
echo ""

# Import MusicRelease data
echo "Importing MusicReleases..."
curl -X POST "$BASE_URL/api/seed/music-releases/import" -H "Content-Type: application/json"
echo ""

echo "Data population complete!"
