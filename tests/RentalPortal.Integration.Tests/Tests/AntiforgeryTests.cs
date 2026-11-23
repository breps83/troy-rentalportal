using System.Net;
using RentalPortal.Integration.Tests.Infrastructure;

namespace RentalPortal.Integration.Tests.Tests;

/// <summary>
/// AutoValidateAntiforgeryToken is a global filter, so every POST needs the token.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AntiforgeryTests(IntegrationFixture fixture)
{
    private readonly IntegrationAppFactory _app = fixture.App;

    private readonly TestData _data = fixture.Data;

    [Fact]
    public async Task Login_WithoutToken_IsRejected()
    {
        var client = _app.CreateNoRedirectClient();

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Email"] = TestUsers.Applicant,
                ["Password"] = TestUsers.Password
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProperty_WithoutToken_IsRejected()
    {
        var client = _app.CreateNoRedirectClient();
        await client.LoginAsync(TestUsers.Manager);
        var name = TestData.Unique("No Token");

        var response = await client.PostAsync("/Properties/Create", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Name"] = name,
                ["Street"] = "1 Main St",
                ["City"] = "Springfield",
                ["State"] = "IL",
                ["PostalCode"] = "62701"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(await _data.PropertyExistsAsync(name));
    }
}
