using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace RentalPortal.Integration.Tests.Infrastructure;

/// <summary>
/// Runs the app in memory against its own database (RentalPortalIntegration).
/// </summary>
public class IntegrationAppFactory : WebApplicationFactory<Program>
{
    public const string DatabaseName = "RentalPortalIntegration";

    // RENTALPORTAL_TEST_SQL=<connection string> uses another SQL Server instead of LocalDB.
    // The database name is always replaced, because each run drops and recreates this database.
    public static readonly string ConnectionString = BuildConnectionString();

    private static string BuildConnectionString()
    {
        var server = Environment.GetEnvironmentVariable("RENTALPORTAL_TEST_SQL");

        if (string.IsNullOrWhiteSpace(server))
            server = "Server=(localdb)\\mssqllocaldb;Trusted_Connection=True";

        var builder = new SqlConnectionStringBuilder(server)
        {
            InitialCatalog = DatabaseName
        };

        return builder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
    }

    /// <summary>
    /// A client that keeps cookies and doesn't follow redirects, so tests can check where a request was sent.
    /// </summary>
    public HttpClient CreateNoRedirectClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
}
