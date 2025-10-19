#!/bin/bash

# Test script for Discogs API integration
# Usage: ./test-discogs.sh [catalog-number]

CATALOG_NUMBER=${1:-"CAT001"}
BASE_URL="http://localhost:5072"

echo "Testing Discogs Integration..."
echo "================================"
echo ""

# Test 1: Search by catalog number
echo "1. Testing search endpoint with catalog number: $CATALOG_NUMBER"
curl -s "${BASE_URL}/api/discogs/search?catalogNumber=${CATALOG_NUMBER}" | jq '.'
echo ""
echo ""

# Test 2: Search with filters
echo "2. Testing search with filters (Vinyl, US, 2020)"
curl -s "${BASE_URL}/api/discogs/search?catalogNumber=${CATALOG_NUMBER}&format=Vinyl&country=US&year=2020" | jq '.'
echo ""
echo ""

# If a release ID is provided as second argument, test release details
if [ ! -z "$2" ]; then
    RELEASE_ID=$2
    echo "3. Testing release details for ID: $RELEASE_ID"
    curl -s "${BASE_URL}/api/discogs/release/${RELEASE_ID}" | jq '.'
    echo ""
fi

echo ""
echo "Tests complete!"
