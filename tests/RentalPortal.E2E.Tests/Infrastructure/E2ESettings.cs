using System.Reflection;

namespace RentalPortal.E2E.Tests.Infrastructure;

/// <summary>
/// Run settings read from environment variables. Defaults suit Visual Studio Test Explorer:
/// headless, no video, no trace. run-e2e.ps1 (or e2e-video.runsettings) turns recording on.
/// </summary>
public static class E2ESettings
{
    public const string DatabaseName = "RentalPortalE2E";

    public const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=" + DatabaseName +
        ";Trusted_Connection=True;MultipleActiveResultSets=true";

    // E2E_VIDEO=1 records a video of each test, with annotations.
    public static bool RecordVideo => IsOn("E2E_VIDEO");

    // E2E_TRACE=1 saves a Playwright trace of each test. On by default when recording video.
    public static bool RecordTrace => IsOn("E2E_TRACE") || RecordVideo;

    // E2E_HEADED=1 shows the browser window.
    public static bool Headed => IsOn("E2E_HEADED");

    // Annotations (chapter titles, requirement banner, action highlights) only matter when someone watches.
    public static bool Annotate => RecordVideo || Headed;

    // E2E_SLOWMO=<ms> slows each browser action so videos are easy to follow. Defaults to 600 ms when annotating.
    public static float SlowMo
    {
        get
        {
            var value = Environment.GetEnvironmentVariable("E2E_SLOWMO");

            if (float.TryParse(value, out var milliseconds))
                return milliseconds;

            return Annotate ? 600 : 0;
        }
    }

    private static readonly Lazy<string> _outputDirectory = new Lazy<string>(CreateOutputDirectory);

    // E2E_OUTPUT=<folder> sets where videos and traces go; defaults to TestResults/e2e/<timestamp> at the repo root.
    public static string OutputDirectory => _outputDirectory.Value;

    private static bool IsOn(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateOutputDirectory()
    {
        var folder = Environment.GetEnvironmentVariable("E2E_OUTPUT");

        if (string.IsNullOrWhiteSpace(folder))
        {
            var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            folder = Path.Combine(FindRepositoryRoot(), "TestResults", "e2e", stamp);
        }

        Directory.CreateDirectory(folder);

        return folder;
    }

    private static string FindRepositoryRoot()
    {
        // Set at build time in the .csproj, so this works when the tests are built to a temp folder.
        var attributes = typeof(E2ESettings).Assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);

        foreach (AssemblyMetadataAttribute attribute in attributes)
        {
            if (attribute.Key == "RepositoryRoot" && Directory.Exists(attribute.Value))
                return attribute.Value!;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RentalPortal.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }
}
