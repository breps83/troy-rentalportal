using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;

namespace RentalPortal.Integration.Tests.Infrastructure;

/// <summary>
/// Drops the integration database and starts the app once per run. Startup migrates and seeds it.
/// </summary>
public class IntegrationFixture : IAsyncLifetime
{
    public IntegrationAppFactory App { get; } = new IntegrationAppFactory();

    public TestData Data { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(IntegrationAppFactory.ConnectionString)
            .Options;

        using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
        }

        Data = new TestData(App.Services);
    }

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
    public const string Name = "Integration";
}
