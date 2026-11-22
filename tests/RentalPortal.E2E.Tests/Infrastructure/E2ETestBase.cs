using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit.Abstractions;
using static Microsoft.Playwright.Assertions;

namespace RentalPortal.E2E.Tests.Infrastructure;

/// <summary>
/// Base class for every E2E test. Gives each test a fresh browser context and page, logs in, and
/// records the video and trace when they're turned on (see <see cref="E2ESettings"/>).
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public abstract class E2ETestBase : IAsyncLifetime
{
    // How long each step's chapter card shows, and how long the last frame is held, in videos.
    private const int ChapterMilliseconds = 1500;
    private const int EndHoldMilliseconds = 1500;

    private readonly ITestOutputHelper _output;
    private readonly List<string> _pageErrors = new List<string>();
    private IAsyncDisposable? _requirementBanner;
    private bool _stepOpen;
    private bool _allowPageErrors;

    protected E2ETestBase(E2EFixture fixture, ITestOutputHelper output)
    {
        Fixture = fixture;
        _output = output;
        TestName = GetTestName(output);
    }

    protected E2EFixture Fixture { get; }

    protected TestData Data => Fixture.Data;

    protected IBrowserContext Context { get; private set; } = null!;

    protected IPage Page { get; private set; } = null!;

    protected Modal Modal { get; private set; } = null!;

    protected string TestName { get; }

    public async Task InitializeAsync()
    {
        var options = new BrowserNewContextOptions
        {
            BaseURL = Fixture.App.BaseUrl,
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            // US formats for currency and dates, matching the app's text.
            Locale = "en-US"
        };

        if (E2ESettings.RecordVideo)
        {
            options.RecordVideoDir = Path.Combine(E2ESettings.OutputDirectory, "raw");
            options.RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 800 };
        }

        Context = await Fixture.Browser.NewContextAsync(options);
        Context.SetDefaultTimeout(10_000);

        if (E2ESettings.RecordTrace)
        {
            await Context.Tracing.StartAsync(new TracingStartOptions
            {
                Title = TestName,
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
        }

        Page = await Context.NewPageAsync();
        Modal = new Modal(Page);

        Page.PageError += (_, error) => _pageErrors.Add($"Script error: {error}");
        Page.Response += (_, response) =>
        {
            if (response.Status >= 500)
                _pageErrors.Add($"Server error {response.Status}: {response.Url}");
        };

        if (E2ESettings.Annotate)
        {
            // Playwright's built-in annotations: highlights each clicked or filled element in the video.
            await Page.Screencast.ShowActionsAsync(FieldExtensions.ActionAnnotations);
        }
    }

    public async Task DisposeAsync()
    {
        // Hold the final state so the video doesn't end on the last action.
        if (E2ESettings.Annotate && !Page.IsClosed)
            await Page.WaitForTimeoutAsync(EndHoldMilliseconds);

        if (_stepOpen && E2ESettings.RecordTrace)
            await Context.Tracing.GroupEndAsync();

        var fileName = SafeFileName(TestName);

        if (E2ESettings.RecordTrace)
        {
            var tracePath = Path.Combine(E2ESettings.OutputDirectory, "traces", fileName + ".zip");
            await Context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
            _output.WriteLine($"Trace: {tracePath}");
        }

        var video = Page.Video;

        await Context.CloseAsync();

        if (video != null)
        {
            var videoPath = Path.Combine(E2ESettings.OutputDirectory, "videos", fileName + ".webm");
            await video.SaveAsAsync(videoPath);
            await video.DeleteAsync();
            _output.WriteLine($"Video: {videoPath}");
        }

        // Fails the test (as a cleanup failure) if the page threw a script error or the server returned a 5xx.
        if (_pageErrors.Count > 0 && !_allowPageErrors)
            throw new InvalidOperationException("The page reported errors:\n" + string.Join("\n", _pageErrors));
    }

    /// <summary>
    /// Marks the start of a step that verifies a requirement. Shows a chapter title and a banner in
    /// the video, groups the actions that follow in the trace, and writes the step to the test output.
    /// </summary>
    /// <param name="requirement">The checklist ID, for example "FR 3.b", or a short area name.</param>
    /// <param name="description">What this step checks, in plain words.</param>
    protected async Task Step(string requirement, string description)
    {
        _output.WriteLine($"[{requirement}] {description}");

        if (E2ESettings.RecordTrace)
        {
            if (_stepOpen)
                await Context.Tracing.GroupEndAsync();

            await Context.Tracing.GroupAsync($"{requirement}: {description}");
            _stepOpen = true;
        }

        if (!E2ESettings.Annotate)
            return;

        if (_requirementBanner != null)
            await _requirementBanner.DisposeAsync();

        _requirementBanner = await Page.Screencast.ShowOverlayAsync(BannerHtml(requirement, description));

        var shown = System.Diagnostics.Stopwatch.StartNew();

        await Page.Screencast.ShowChapterAsync(requirement, new ScreencastShowChapterOptions
        {
            Description = description,
            Duration = ChapterMilliseconds
        });

        // Let the viewer read the card before the step's actions start behind it.
        var remaining = ChapterMilliseconds - (int)shown.ElapsedMilliseconds;

        if (remaining > 0)
            await Page.WaitForTimeoutAsync(remaining);
    }

    /// <summary>
    /// Logs in through the login page.
    /// </summary>
    protected async Task LoginAsAsync(string email, string password = TestUsers.Password)
    {
        await Page.GotoAsync("/Account/Login");
        await Page.GetByLabel("Email").EnterAsync(email);
        await Page.GetByLabel("Password").EnterSecretAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Log out" })).ToBeVisibleAsync();
    }

    /// <summary>
    /// Logs out from the navbar, then logs in as another user.
    /// </summary>
    protected async Task SwitchUserAsync(string email)
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log out" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Log out" })).ToBeHiddenAsync();
        await LoginAsAsync(email);
    }

    /// <summary>
    /// The navbar, so links like "Properties" aren't confused with page buttons such as "Manage Properties".
    /// </summary>
    protected ILocator NavBar => Page.Locator("header nav");

    /// <summary>
    /// Posts a form the way a crafted request would, bypassing the UI. Uses the browser's cookies and
    /// the antiforgery token from the current page. Redirects aren't followed, so the response can be checked.
    /// </summary>
    protected async Task<IAPIResponse> PostFormAsync(string url, Dictionary<string, string> fields)
    {
        var token = await Page.Locator("input[name=__RequestVerificationToken]").First.InputValueAsync();

        var form = Context.APIRequest.CreateFormData();
        form.Set("__RequestVerificationToken", token);

        foreach (var field in fields)
        {
            form.Set(field.Key, field.Value);
        }

        return await Context.APIRequest.PostAsync(url, new() { Form = form, MaxRedirects = 0 });
    }

    /// <summary>
    /// Use in tests that deliberately cause a server error. Script errors and 5xx responses then
    /// don't fail the test.
    /// </summary>
    protected void AllowPageErrors()
    {
        _allowPageErrors = true;
    }

    /// <summary>
    /// The alert shown at the top of the page for TempData messages (_Alerts.cshtml).
    /// </summary>
    protected ILocator Alert(string text) =>
        Page.Locator("main > .alert").Filter(new() { HasText = text });

    private static string BannerHtml(string requirement, string description)
    {
        var text = WebUtility.HtmlEncode(requirement) + " &middot; " + WebUtility.HtmlEncode(description);

        return "<div style=\"position:fixed;left:12px;bottom:12px;max-width:70%;padding:6px 12px;" +
               "border-radius:6px;background:rgba(13,110,253,.92);color:#fff;" +
               "font:600 15px system-ui,sans-serif;box-shadow:0 2px 8px rgba(0,0,0,.3)\">" +
               text + "</div>";
    }

    private static string SafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name;
    }

    // xUnit 2 doesn't expose the running test's name, so read it from the output helper.
    // Falls back to a unique name if xUnit's internals change.
    private string GetTestName(ITestOutputHelper output)
    {
        var field = output.GetType().GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
        var test = field?.GetValue(output) as ITest;

        if (test != null)
        {
            var method = test.TestCase.TestMethod;
            var name = method.TestClass.Class.Name.Split('.').Last() + "." + method.Method.Name;

            // A Theory runs once per case; add the case's values so each case gets its own video,
            // for example "WithdrawTests.Applicant_WithdrawsOpenApplication_Draft". File names keep to
            // letters, digits, '.', '_' and '-' so links to them work without encoding.
            var open = test.DisplayName.IndexOf('(');

            if (open >= 0)
            {
                var arguments = test.DisplayName.Substring(open + 1).TrimEnd(')');

                foreach (var argument in arguments.Split(", "))
                {
                    var value = argument.Substring(argument.IndexOf(": ") + 2).Replace("\"", "");
                    name += "_" + Regex.Replace(value, "[^A-Za-z0-9.-]", "-");
                }
            }

            return name;
        }

        return GetType().Name + "." + Guid.NewGuid().ToString("N")[..8];
    }
}
