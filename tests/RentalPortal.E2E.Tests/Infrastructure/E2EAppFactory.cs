using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RentalPortal.Web.Data;

namespace RentalPortal.E2E.Tests.Infrastructure;

/// <summary>
/// Runs the real app in-process on Kestrel (a real port, so a browser can reach it) against its own
/// database. The dev database (RentalPortalDb) is never touched.
/// </summary>
public class E2EAppFactory : WebApplicationFactory<Program>
{
    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Drops the E2E database, then starts the app. The app's normal startup (DbSeeder.SeedAsync)
    /// migrates and seeds the fresh database.
    /// </summary>
    public async Task StartAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(E2ESettings.ConnectionString)
            .Options;

        using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
        }

        // Port 0 lets Kestrel pick a free port.
        UseKestrel(0);
        StartServer();

        var addresses = Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        BaseUrl = addresses!.Addresses.First().TrimEnd('/');
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", E2ESettings.ConnectionString);

        // HTTP only: no dev certificate needed, and UseHttpsRedirection has no HTTPS port to redirect to.
        builder.UseSetting("urls", "http://127.0.0.1:0");

        // Keep the test output readable: warnings and errors only (no SQL for every request).
        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
    }
}
