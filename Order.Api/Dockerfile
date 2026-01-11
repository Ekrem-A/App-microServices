# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Order.Api/Order.Api.csproj", "Order.Api/"]
COPY ["Order.Application/Order.Application.csproj", "Order.Application/"]
COPY ["Order.Domain/Order.Domain.csproj", "Order.Domain/"]
COPY ["Order.Infrastructure/Order.Infrastructure.csproj", "Order.Infrastructure/"]

RUN dotnet restore "Order.Api/Order.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Order.Api"
RUN dotnet build "Order.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Order.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy published files
COPY --from=publish /app/publish .

# Set environment - Railway will provide PORT
ENV ASPNETCORE_ENVIRONMENT=Production

# Don't set ASPNETCORE_URLS - let the app handle PORT via RailwayExtensions

ENTRYPOINT ["dotnet", "Order.Api.dll"]
