# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Payment.Api/Payment.Api.csproj", "Payment.Api/"]
COPY ["Payment.Application/Payment.Application.csproj", "Payment.Application/"]
COPY ["Payment.Domain/Payment.Domain.csproj", "Payment.Domain/"]
COPY ["Payment.Infrastructure/Payment.Infrastructure.csproj", "Payment.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Payment.Api/Payment.Api.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR /src/Payment.Api
RUN dotnet build "Payment.Api.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "Payment.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published app
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Payment.Api.dll"]
