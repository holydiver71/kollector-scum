#!/bin/bash
# Build Docker image for KollectorScum API
# Usage: ./build-docker.sh [tag]

set -e

# Default tag
TAG="${1:-latest}"
IMAGE_NAME="kollector-scum-backend"

echo "Building Docker image: $IMAGE_NAME:$TAG"
echo "Build context: $(pwd)"
echo ""

# Build from the backend directory
docker build -t "$IMAGE_NAME:$TAG" -f Dockerfile .

echo ""
echo "✅ Build complete!"
echo "Image: $IMAGE_NAME:$TAG"
echo ""
echo "To run the container:"
echo "  docker run --rm -it -p 8080:8080 --env-file .env $IMAGE_NAME:$TAG"
echo ""
echo "To test with minimal config:"
echo "  docker run --rm -it -p 8080:8080 \\"
echo "    -e ASPNETCORE_ENVIRONMENT=Development \\"
echo "    -e \"ConnectionStrings__DefaultConnection=Host=localhost;Database=kollector_scum;Username=postgres;Password=postgres\" \\"
echo "    $IMAGE_NAME:$TAG"
