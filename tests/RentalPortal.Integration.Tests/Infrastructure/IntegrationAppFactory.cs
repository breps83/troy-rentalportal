using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace RentalPortal.Integration.Tests.Infrastructure;

/// <summary>
/// Runs the app in memory against its own database (RentalPortalIntegration).
/// </summary>
public class IntegrationAppFactory : WebApplicationFactory<Program>
{
    public const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=RentalPortalIntegration" +
        ";Trusted_Connection=True;MultipleActiveResultSets=true";

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
