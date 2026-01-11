#!/bin/bash
# Script to test the local storage migration
# This script starts the API, waits for it to be ready, and calls the migration endpoint

set -e

echo "Starting API in background..."
cd backend/KollectorScum.Api
dotnet run > /tmp/kollector-api.log 2>&1 &
API_PID=$!

echo "Waiting for API to be ready..."
for i in {1..30}; do
    if curl -s http://localhost:5072/health > /dev/null 2>&1; then
        echo "API is ready!"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "API failed to start within 30 seconds"
        kill $API_PID 2>/dev/null || true
        exit 1
    fi
    sleep 1
done

echo ""
echo "API is running. You can now:"
echo "1. Login to get an auth token"
echo "2. Call the migration endpoint: POST http://localhost:5072/api/admin/migrate-local-storage"
echo ""
echo "API PID: $API_PID"
echo "API logs: tail -f /tmp/kollector-api.log"
echo ""
echo "To stop the API: kill $API_PID"
