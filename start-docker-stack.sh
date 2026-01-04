#!/bin/bash
# Quick start script for running the full stack locally with Docker backend

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}=== Kollector Scum - Docker Stack Startup ===${NC}"
echo ""

# Check if backend .env exists
if [ ! -f backend/.env ]; then
    echo -e "${YELLOW}⚠ Warning: backend/.env not found${NC}"
    echo "  Copy backend/.env.example and configure it"
    exit 1
fi

# Check if frontend .env.local exists
if [ ! -f frontend/.env.local ]; then
    echo -e "${YELLOW}⚠ Warning: frontend/.env.local not found${NC}"
    echo "  Copy frontend/.env.example and configure it"
    exit 1
fi

echo -e "${GREEN}Starting Backend (Docker)...${NC}"
echo "  Backend will run on: http://localhost:8080"
echo ""

# Start backend in background
cd backend
docker run -d --name kollector-api -p 8080:8080 --env-file .env kollector-scum-backend:latest
cd ..

# Wait for backend to be ready
echo "Waiting for backend to be ready..."
sleep 3

# Check if backend is up
if curl -s http://localhost:8080/health > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Backend is running${NC}"
else
    echo -e "${YELLOW}⚠ Backend health check failed (it might still be starting)${NC}"
fi

echo ""
echo -e "${GREEN}Starting Frontend (Next.js)...${NC}"
echo "  Frontend will run on: http://localhost:3000"
echo ""

# Start frontend
cd frontend
npm run dev &
FRONTEND_PID=$!
cd ..

echo ""
echo -e "${GREEN}=== Stack Started ===${NC}"
echo ""
echo "Backend:  http://localhost:8080"
echo "Frontend: http://localhost:3000"
echo ""
echo "To stop:"
echo "  docker stop kollector-api && docker rm kollector-api"
echo "  kill $FRONTEND_PID"
echo ""
echo "Or use: ./stop-docker-stack.sh"
echo ""
