using System.Net;
using RentalPortal.Integration.Tests.Infrastructure;
using RentalPortal.Web.Enums;

namespace RentalPortal.Integration.Tests.Tests;

/// <summary>
/// Applications are kept as records (DeleteBehavior.Restrict), so deleting a unit or property that has one
/// shows a message in the confirm modal instead of failing in the database.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class DeleteTests(IntegrationFixture fixture)
{
    private readonly IntegrationAppFactory _app = fixture.App;

    private readonly TestData _data = fixture.Data;

    [Fact]
    public async Task UnitWithApplication_ShowsMessageAndIsKept()
    {
        var unit = await _data.CreateUnitAsync();
        await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Withdrawn, unit.Id);
        var client = await LoginManagerAsync();

        var confirm = await client.GetStringAsync($"/Units/Delete/{unit.Id}");
        Assert.Contains("This unit has rental applications and can&#x27;t be deleted.", confirm);
        Assert.DoesNotContain("type=\"submit\"", confirm);

        var response = await client.PostFormAsync($"/Units/Delete/{unit.Id}", []);
        await AssertBlockedAsync(response, "This unit has rental applications and can&#x27;t be deleted.");
        Assert.True(await _data.UnitExistsAsync(unit.Id));
    }

    [Fact]
    public async Task PropertyWithApplication_ShowsMessageAndIsKept()
    {
        var unit = await _data.CreateUnitAsync();
        await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Draft, unit.Id);
        var client = await LoginManagerAsync();

        var response = await client.PostFormAsync($"/Properties/Delete/{unit.PropertyId}", []);

        await AssertBlockedAsync(response,
            "This property has units with rental applications and can&#x27;t be removed.");
        Assert.True(await _data.PropertyExistsAsync(unit.PropertyId));
    }

    [Fact]
    public async Task UnitWithoutApplications_IsDeleted()
    {
        var unit = await _data.CreateUnitAsync();
        var client = await LoginManagerAsync();

        var response = await client.PostFormAsync($"/Units/Delete/{unit.Id}", []);

        Assert.Equal("{\"success\":true}", await response.Content.ReadAsStringAsync());
        Assert.False(await _data.UnitExistsAsync(unit.Id));
    }

    [Fact]
    public async Task PropertyWithoutApplications_IsDeletedWithItsUnits()
    {
        var unit = await _data.CreateUnitAsync();
        var client = await LoginManagerAsync();

        var response = await client.PostFormAsync($"/Properties/Delete/{unit.PropertyId}", []);

        Assert.Equal("{\"success\":true}", await response.Content.ReadAsStringAsync());
        Assert.False(await _data.PropertyExistsAsync(unit.PropertyId));
        Assert.False(await _data.UnitExistsAsync(unit.Id));
    }

    private async Task<HttpClient> LoginManagerAsync()
    {
        var client = _app.CreateNoRedirectClient();
        await client.LoginAsync(TestUsers.Manager);

        return client;
    }

    private static async Task AssertBlockedAsync(HttpResponseMessage response, string message)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains(message, await response.Content.ReadAsStringAsync());
    }
}
