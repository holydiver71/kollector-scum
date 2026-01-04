#!/bin/bash
# Build script for KollectorScum backend - builds both .NET and Docker image

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
SKIP_DOTNET_BUILD=false
TAG="latest"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-dotnet)
            SKIP_DOTNET_BUILD=true
            shift
            ;;
        --tag)
            TAG="$2"
            shift 2
            ;;
        --help)
            echo "Usage: ./build-docker.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --skip-dotnet    Skip the local dotnet build (Docker will still build)"
            echo "  --tag TAG        Docker image tag (default: latest)"
            echo "  --help           Show this help message"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            exit 1
            ;;
    esac
done

echo -e "${GREEN}=== KollectorScum Backend Build ===${NC}"
echo ""

# Step 1: Optional local .NET build for quick validation
if [ "$SKIP_DOTNET_BUILD" = false ]; then
    echo -e "${YELLOW}Step 1: Building .NET project locally...${NC}"
    dotnet build KollectorScum.Api/KollectorScum.Api.csproj -c Release
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ .NET build successful${NC}"
    else
        echo -e "${RED}✗ .NET build failed${NC}"
        exit 1
    fi
    echo ""
else
    echo -e "${YELLOW}Step 1: Skipping local .NET build${NC}"
    echo ""
fi

# Step 2: Build Docker image
echo -e "${YELLOW}Step 2: Building Docker image...${NC}"
docker build -t kollector-scum-backend:$TAG .
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Docker build successful${NC}"
else
    echo -e "${RED}✗ Docker build failed${NC}"
    exit 1
fi
echo ""

# Step 3: Show image info
echo -e "${GREEN}=== Build Complete ===${NC}"
echo ""
echo "Docker image: kollector-scum-backend:$TAG"
echo ""
docker images kollector-scum-backend:$TAG
echo ""

# Step 4: Check for .env file
if [ ! -f .env ]; then
    echo -e "${YELLOW}⚠ Warning: No .env file found${NC}"
    echo "  Copy .env.example to .env and configure it:"
    echo "  cp .env.example .env"
    echo ""
fi

# Step 5: Helpful commands
echo -e "${YELLOW}Next steps:${NC}"
echo ""
echo "To run the container locally (requires .env file):"
echo "  docker run --rm -it -p 8080:8080 --env-file .env kollector-scum-backend:$TAG"
echo ""
echo "To run in detached mode:"
echo "  docker run -d --name kollector-api -p 8080:8080 --env-file .env kollector-scum-backend:$TAG"
echo ""
echo "To stop a detached container:"
echo "  docker stop kollector-api && docker rm kollector-api"
echo ""
echo "To view logs:"
echo "  docker logs -f kollector-api"
echo ""
echo "To push to a registry (e.g., Docker Hub, GitHub Container Registry):"
echo "  docker tag kollector-scum-backend:$TAG your-registry/kollector-scum-backend:$TAG"
echo "  docker push your-registry/kollector-scum-backend:$TAG"
echo ""
