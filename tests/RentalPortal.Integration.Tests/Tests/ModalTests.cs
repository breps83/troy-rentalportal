using System.Net;
using RentalPortal.Integration.Tests.Infrastructure;

namespace RentalPortal.Integration.Tests.Tests;

/// <summary>
/// Modal forms (site.js): an invalid POST returns the partial view with errors, a valid one returns
/// { success: true }.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class ModalTests(IntegrationFixture fixture)
{
    private readonly IntegrationAppFactory _app = fixture.App;

    private readonly TestData _data = fixture.Data;

    [Fact]
    public async Task InvalidProperty_ReturnsFormWithErrors()
    {
        var client = await LoginManagerAsync();
        var name = TestData.Unique("Invalid");

        var response = await client.PostFormAsync("/Properties/Create", new Dictionary<string, string>
        {
            ["Name"] = name,
            ["Street"] = "1 Main St",
            ["City"] = "Springfield",
            ["State"] = "I1",
            ["PostalCode"] = "6270"
        });

        var html = await AssertPartialAsync(response);
        Assert.Contains("Please enter a valid state abbreviation.", html);
        Assert.Contains("Please enter a valid postal code.", html);
        Assert.False(await _data.PropertyExistsAsync(name));
    }

    [Fact]
    public async Task ValidProperty_ReturnsSuccess()
    {
        var client = await LoginManagerAsync();
        var name = TestData.Unique("Valid");

        var response = await client.PostFormAsync("/Properties/Create", new Dictionary<string, string>
        {
            ["Name"] = name,
            ["Street"] = "1 Main St",
            ["City"] = "Springfield",
            ["State"] = "IL",
            ["PostalCode"] = "62701"
        });

        Assert.Equal("{\"success\":true}", await response.Content.ReadAsStringAsync());
        Assert.True(await _data.PropertyExistsAsync(name));
    }

    [Fact]
    public async Task DuplicateUnitNumber_ReturnsFormWithError()
    {
        var unit = await _data.CreateUnitAsync();
        var unitTypeId = unit.UnitTypeId.ToString();
        var client = await LoginManagerAsync();

        var response = await client.PostFormAsync("/Units/Create", new Dictionary<string, string>
        {
            ["PropertyId"] = unit.PropertyId.ToString(),
            ["UnitNumber"] = unit.UnitNumber,
            ["Bedrooms"] = "1",
            ["MonthlyRent"] = "1200",
            ["UnitTypeId"] = unitTypeId
        });

        var html = await AssertPartialAsync(response);
        Assert.Contains("This property already has a unit with that number.", html);
        Assert.Contains($"value=\"{unitTypeId}\"", html);
    }

    private async Task<HttpClient> LoginManagerAsync()
    {
        var client = _app.CreateNoRedirectClient();
        await client.LoginAsync(TestUsers.Manager);

        return client;
    }

    private static async Task<string> AssertPartialAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);

        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<html", html);

        return html;
    }
}
