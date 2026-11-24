using System.Net;
using RentalPortal.Integration.Tests.Infrastructure;
using RentalPortal.Web.Domain;

namespace RentalPortal.Integration.Tests.Tests;

/// <summary>
/// Sign-up and sign-in through AccountController with the app's real Identity setup (FR 1.a).
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AccountTests(IntegrationFixture fixture)
{
    private const string InvalidLoginMessage = "Invalid login attempt.";

    private readonly IntegrationAppFactory _app = fixture.App;

    private readonly TestData _data = fixture.Data;

    [Fact]
    public async Task Register_AsPropertyManager_AddsRole_AndSignsIn()
    {
        var client = _app.CreateNoRedirectClient();
        var email = NewEmail();

        var response = await RegisterAsync(client, email, "PropertyManager");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
        Assert.Equal([Roles.PropertyManager], await _data.GetRolesAsync(email));

        // signed in: the properties page opens instead of redirecting to login
        var properties = await client.GetAsync("/Properties");
        Assert.Equal(HttpStatusCode.OK, properties.StatusCode);
    }

    /// <summary>
    /// A forged account type, such as a number outside the enum, gets no account. Model binding rejects it
    /// first; the controller's own check is a second guard, and its message isn't shown.
    /// </summary>
    [Fact]
    public async Task Register_WithForgedAccountType_IsRejected()
    {
        var client = _app.CreateNoRedirectClient();
        var email = NewEmail();

        var response = await RegisterAsync(client, email, "99");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The value &#x27;99&#x27; is invalid.", await response.Content.ReadAsStringAsync());
        Assert.Null(await _data.GetRolesAsync(email));
    }

    [Fact]
    public async Task Register_WithEmailInUse_ShowsIdentityError()
    {
        var client = _app.CreateNoRedirectClient();

        var response = await RegisterAsync(client, TestUsers.Applicant, "Applicant");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("is already taken.", await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// "password" passes the view model (8 characters) but not Identity's password rules.
    /// </summary>
    [Fact]
    public async Task Register_WithWeakPassword_ShowsIdentityError_AndCreatesNoUser()
    {
        var client = _app.CreateNoRedirectClient();
        var email = NewEmail();

        var response = await RegisterAsync(client, email, "Applicant", password: "password");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Passwords must have at least one digit", await response.Content.ReadAsStringAsync());
        Assert.Null(await _data.GetRolesAsync(email));
    }

    [Fact]
    public async Task Login_WithMissingFields_ShowsForm()
    {
        var client = _app.CreateNoRedirectClient();

        var response = await LoginAsync(client, "", "");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<form", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShowsInvalidLogin()
    {
        var client = _app.CreateNoRedirectClient();

        var response = await LoginAsync(client, NewEmail(), TestUsers.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(InvalidLoginMessage, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsInvalidLogin()
    {
        var client = _app.CreateNoRedirectClient();
        var email = await _data.CreateApplicantAsync();

        var response = await LoginAsync(client, email, "Wrong-Password1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(InvalidLoginMessage, await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Identity locks an account after five failed attempts; then even the right password is refused.
    /// </summary>
    [Fact]
    public async Task Login_AfterFiveFailures_IsLockedOut()
    {
        var client = _app.CreateNoRedirectClient();
        var email = await _data.CreateApplicantAsync();

        for (var i = 0; i < 5; i++)
            await LoginAsync(client, email, "Wrong-Password1");

        var response = await LoginAsync(client, email, TestUsers.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("This account is locked.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_WithLocalReturnUrl_RedirectsThere()
    {
        var client = _app.CreateNoRedirectClient();

        var response = await LoginAsync(client, TestUsers.Manager, TestUsers.Password, "/Properties");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Properties", response.Headers.Location?.OriginalString);
    }

    /// <summary>A return URL to another site is ignored, so the login page can't be used to redirect away.</summary>
    [Fact]
    public async Task Login_WithExternalReturnUrl_RedirectsHome()
    {
        var client = _app.CreateNoRedirectClient();

        var response = await LoginAsync(client, TestUsers.Manager, TestUsers.Password, "https://example.com/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    private static string NewEmail() => $"{Guid.NewGuid():N}@rentalportal.test";

    private static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string email,
        string accountType,
        string password = TestUsers.Password) =>
        client.PostFormAsync("/Account/Register", "/Account/Register", new Dictionary<string, string>
        {
            ["FirstName"] = "Jordan",
            ["LastName"] = "Tester",
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["AccountType"] = accountType
        });

    private static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        string password,
        string? returnUrl = null)
    {
        var fields = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password
        };

        if (returnUrl != null)
            fields["ReturnUrl"] = returnUrl;

        return client.PostFormAsync("/Account/Login", "/Account/Login", fields);
    }
}
