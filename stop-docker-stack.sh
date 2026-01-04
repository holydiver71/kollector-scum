#!/bin/bash
# Stop the Docker-based stack

set -e

GREEN='\033[0;32m'
NC='\033[0m'

echo -e "${GREEN}Stopping Kollector Scum Docker Stack...${NC}"
echo ""

# Stop and remove backend container
if docker ps -a | grep -q kollector-api; then
    echo "Stopping backend container..."
    docker stop kollector-api
    docker rm kollector-api
    echo -e "${GREEN}✓ Backend stopped${NC}"
else
    echo "Backend container not running"
fi

# Kill frontend (you might need to do this manually)
echo ""
echo "To stop frontend, press Ctrl+C in its terminal or:"
echo "  pkill -f 'next dev'"
echo ""
