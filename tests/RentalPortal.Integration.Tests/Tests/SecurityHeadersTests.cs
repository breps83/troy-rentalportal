using System.Net;
using RentalPortal.Integration.Tests.Infrastructure;

namespace RentalPortal.Integration.Tests.Tests;

/// <summary>
/// Program.cs adds the security headers to every response: pages, static files, and error pages.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class SecurityHeadersTests(IntegrationFixture fixture)
{
    private readonly IntegrationAppFactory _app = fixture.App;

    [Theory]
    [InlineData("/Account/Login", HttpStatusCode.OK)]
    [InlineData("/css/site.css", HttpStatusCode.OK)]
    [InlineData("/no-such-page", HttpStatusCode.NotFound)]
    public async Task Response_HasSecurityHeaders(string url, HttpStatusCode expectedStatus)
    {
        var client = _app.CreateNoRedirectClient();

        var response = await client.GetAsync(url);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
        Assert.Equal("strict-origin-when-cross-origin", Header(response, "Referrer-Policy"));

        var csp = Header(response, "Content-Security-Policy");
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("script-src 'self';", csp);
        Assert.Contains("style-src 'self';", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.DoesNotContain("unsafe-inline", csp);
    }

    [Fact]
    public async Task SignedInPage_HasSecurityHeaders()
    {
        var client = _app.CreateNoRedirectClient();
        await client.LoginAsync(TestUsers.Manager);

        var response = await client.GetAsync("/Applications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("script-src 'self';", Header(response, "Content-Security-Policy"));
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
            return string.Join(", ", values);

        if (response.Content.Headers.TryGetValues(name, out values))
            return string.Join(", ", values);

        return string.Empty;
    }
}
