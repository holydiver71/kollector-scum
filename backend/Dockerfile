# syntax=docker/dockerfile:1

# Multi-stage build for the KollectorScum.Api (.NET 8)

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore
COPY backend/KollectorScum.Api/KollectorScum.Api.csproj KollectorScum.Api/
RUN dotnet restore KollectorScum.Api/KollectorScum.Api.csproj

# Build/publish
COPY backend/KollectorScum.Api/ KollectorScum.Api/
RUN dotnet publish KollectorScum.Api/KollectorScum.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# The ASP.NET base image defaults to port 8080; hosting platforms may override via PORT.
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "KollectorScum.Api.dll"]
