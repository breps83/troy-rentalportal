using Microsoft.Playwright;

namespace RentalPortal.E2E.Tests.Infrastructure;

/// <summary>
/// Shared by every E2E test: starts the app (fresh database) and one Chromium browser per run.
/// Each test still gets its own browser context, so cookies and videos don't leak between tests.
/// </summary>
public class E2EFixture : IAsyncLifetime
{
    public E2EAppFactory App { get; } = new E2EAppFactory();

    public IPlaywright Playwright { get; private set; } = null!;

    public IBrowser Browser { get; private set; } = null!;

    public TestData Data { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await App.StartAsync();

        Data = new TestData(App.Services);

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !E2ESettings.Headed,
            SlowMo = E2ESettings.SlowMo
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();
        Playwright.Dispose();
        await App.DisposeAsync();

        // Playwright records into raw/ first; each test then saves its video under its own name.
        if (!E2ESettings.RecordVideo)
            return;

        var raw = Path.Combine(E2ESettings.OutputDirectory, "raw");

        if (Directory.Exists(raw) && !Directory.EnumerateFileSystemEntries(raw).Any())
            Directory.Delete(raw);
    }
}

/// <summary>
/// One collection, so E2E tests share the fixture and run one at a time.
/// </summary>
[CollectionDefinition(Name)]
public class E2ECollection : ICollectionFixture<E2EFixture>
{
    public const string Name = "E2E";
}
