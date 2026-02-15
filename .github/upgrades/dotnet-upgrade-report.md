# .NET 9.0 Upgrade Report

## Project target framework modifications

| Project name                                          | Old Target Framework | New Target Framework | Commits               |
|:------------------------------------------------------|:--------------------:|:--------------------:|:----------------------|
| Idendity.Domain\Idendity.Domain.csproj                | net8.0               | net9.0               | da6b67e1              |
| Idendity.Application\Idendity.Application.csproj      | net8.0               | net9.0               | da6b67e1              |
| Idendity.Infrastructure\Idendity.Infrastructure.csproj| net8.0               | net9.0               | da6b67e1, 3b656b15    |
| Idendity.Api\Idendity.Api.csproj                      | net8.0               | net9.0               | da6b67e1              |

## NuGet Packages

| Package Name                                          | Old Version     | New Version | Commit Id             |
|:------------------------------------------------------|:---------------:|:-----------:|:----------------------|
| Microsoft.AspNetCore.Authentication.JwtBearer         | 8.0.0           | 9.0.13      | da6b67e1              |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore     | 8.0.14          | 9.0.13      | da6b67e1              |
| Microsoft.EntityFrameworkCore.Design                  | 8.0.14          | 9.0.13      | da6b67e1              |
| Microsoft.EntityFrameworkCore.SqlServer               | 8.0.14;8.0.23   | 9.0.13      | da6b67e1              |
| Microsoft.EntityFrameworkCore.Tools                   | 8.0.14          | 9.0.13      | da6b67e1              |
| Microsoft.Extensions.Identity.Core                    | 8.0.0           | 9.0.13      | da6b67e1              |
| Microsoft.Extensions.Http.Resilience                  | -               | 9.4.0       | 3b656b15              |
| Microsoft.Extensions.ServiceDiscovery                 | -               | 9.2.0       | 3b656b15              |
| OpenTelemetry.Exporter.Console                        | -               | 1.15.0      | 3b656b15              |
| OpenTelemetry.Exporter.OpenTelemetryProtocol          | -               | 1.15.0      | 3b656b15              |
| OpenTelemetry.Extensions.Hosting                      | -               | 1.15.0      | 3b656b15              |
| OpenTelemetry.Instrumentation.AspNetCore              | -               | 1.15.0      | 3b656b15              |
| OpenTelemetry.Instrumentation.EntityFrameworkCore     | -               | 1.10.0-beta.1| 3b656b15             |
| OpenTelemetry.Instrumentation.Http                    | -               | 1.15.0      | 3b656b15              |
| OpenTelemetry.Instrumentation.Runtime                 | -               | 1.11.1      | 3b656b15              |
| OpenTelemetry.Instrumentation.StackExchangeRedis      | -               | 1.10.0-beta.1| 3b656b15             |

## All commits

| Commit ID    | Description                                                                      |
|:-------------|:---------------------------------------------------------------------------------|
| f01b59f2     | Commit upgrade plan                                                              |
| da6b67e1     | Upgrade Idendity.Domain\Idendity.Domain.csproj adımı için son değişiklikleri kaydedin |
| 3b656b15     | Added missing OpenTelemetry and ServiceDiscovery packages. Build succeeded.      |

## Project feature upgrades

### Idendity.Infrastructure

- Added missing OpenTelemetry instrumentation packages for .NET 9.0 compatibility:
  - Microsoft.Extensions.Http.Resilience for AddStandardResilienceHandler
  - Microsoft.Extensions.ServiceDiscovery for AddServiceDiscovery
  - OpenTelemetry exporter and instrumentation packages for metrics, tracing, and logging

## Next steps

- Test the application to ensure all functionality works correctly with .NET 9.0
- Consider updating the Dockerfile base image to use .NET 9.0 runtime
- Review and test Docker container builds
