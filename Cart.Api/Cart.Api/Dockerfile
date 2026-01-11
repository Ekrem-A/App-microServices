# Cart.Api Dockerfile
# Multi-stage build for optimized production image

# ============================================
# Base Stage - Runtime image
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user for security
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Install curl for health checks and ICU for globalization
RUN apk add --no-cache curl icu-libs

# ============================================
# Build Stage - SDK image for building
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files first for better layer caching
COPY ["Cart.Api/Cart.Api.csproj", "Cart.Api/"]
COPY ["Cart.Application/Cart.Application.csproj", "Cart.Application/"]
COPY ["Cart.Domain/Cart.Domain.csproj", "Cart.Domain/"]
COPY ["Cart.Infrastructure/Cart.Infrastructure.csproj", "Cart.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "./Cart.Api/Cart.Api.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/Cart.Api"
RUN dotnet build "./Cart.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# ============================================
# Publish Stage - Create publish output
# ============================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cart.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false

# ============================================
# Final Stage - Production runtime image
# ============================================
FROM base AS final
WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

# Set ownership to non-root user
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Environment variables - Railway uses PORT env variable
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check disabled for Railway (Railway has its own health checks)
# HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
#     CMD curl -f http://localhost:${PORT:-8080}/health/live || exit 1

# Labels
LABEL maintainer="Cart.Api Team" \
      version="1.0" \
      description="Cart microservice with CQRS pattern" \
      org.opencontainers.image.source="https://github.com/Ekrem-A/Cart.Service"

# Entry point
ENTRYPOINT ["dotnet", "Cart.Api.dll"]
