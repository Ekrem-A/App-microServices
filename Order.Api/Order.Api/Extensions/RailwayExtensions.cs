namespace Order.Api.Extensions;

public static class RailwayExtensions
{
    /// <summary>
    /// Configures the application for Railway deployment.
    /// Railway sets PORT environment variable for the application.
    /// </summary>
    public static IHostApplicationBuilder ConfigureForRailway(this IHostApplicationBuilder builder)
    {
        // Railway provides DATABASE_URL for PostgreSQL connections
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            // Convert Railway's DATABASE_URL to standard connection string
            var connectionString = ConvertDatabaseUrl(databaseUrl);
            builder.Configuration["ConnectionStrings:OrderDb"] = connectionString;
        }

        // Railway sets PORT environment variable
        var port = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(port))
        {
            builder.Configuration["Kestrel:Endpoints:Http:Url"] = $"http://0.0.0.0:{port}";
        }

        return builder;
    }

    /// <summary>
    /// Converts Railway's DATABASE_URL format to standard PostgreSQL connection string.
    /// DATABASE_URL format: postgresql://user:password@host:port/database
    /// </summary>
    private static string ConvertDatabaseUrl(string databaseUrl)
    {
        try
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo[0];
            var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            // Npgsql 8.x requires camelCase parameter names without spaces
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SslMode=Require;TrustServerCertificate=true";
        }
        catch
        {
            // If conversion fails, return as-is (might already be a connection string)
            return databaseUrl;
        }
    }
}
