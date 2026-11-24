using System.Net;
using RentalPortal.Integration.Tests.Infrastructure;
using RentalPortal.Web.Enums;

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

    [Fact]
    public async Task InvalidPropertyEdit_ReturnsFormWithErrors_AndKeepsProperty()
    {
        var unit = await _data.CreateUnitAsync();
        var newName = TestData.Unique("Renamed");
        var client = await LoginManagerAsync();

        var response = await client.PostFormAsync("/Properties/Edit", new Dictionary<string, string>
        {
            ["Id"] = unit.PropertyId.ToString(),
            ["Name"] = newName,
            ["Street"] = "1 Main St",
            ["City"] = "Springfield",
            ["State"] = "Illinois",
            ["PostalCode"] = "62701"
        });

        var html = await AssertPartialAsync(response);
        Assert.Contains("Please enter a valid state abbreviation.", html);
        Assert.True(await _data.PropertyExistsAsync(unit.Property.Name));
        Assert.False(await _data.PropertyExistsAsync(newName));
    }

    /// <summary>
    /// FR 2.b: an inactive unit type can't be chosen for a unit that doesn't already use it. The form
    /// comes back with its unit type list filled in again.
    /// </summary>
    [Fact]
    public async Task UnitEdit_ToInactiveType_ReturnsFormWithError_AndKeepsType()
    {
        var unit = await _data.CreateUnitAsync();
        var inactiveType = (await _data.GetUnitTypesAsync()).First(t => !t.IsActive);
        var client = await LoginManagerAsync();

        var response = await client.PostFormAsync("/Units/Edit", new Dictionary<string, string>
        {
            ["Id"] = unit.Id.ToString(),
            ["PropertyId"] = unit.PropertyId.ToString(),
            ["UnitNumber"] = unit.UnitNumber,
            ["Bedrooms"] = unit.Bedrooms.ToString(),
            ["MonthlyRent"] = unit.MonthlyRent.ToString(),
            ["UnitTypeId"] = inactiveType.Id.ToString()
        });

        var html = await AssertPartialAsync(response);
        Assert.Contains("This unit type is inactive and can&#x27;t be selected.", html);
        Assert.Contains($"value=\"{unit.UnitTypeId}\"", html);
        Assert.Equal(unit.UnitTypeId, (await _data.GetUnitAsync(unit.Id)).UnitTypeId);
    }

    [Fact]
    public async Task EmptyNote_ReturnsFormWithError()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        var client = await LoginManagerAsync();

        var response = await client.PostFormAsync("/Applications/AddNote", new Dictionary<string, string>
        {
            ["ApplicationId"] = applicationId.ToString(),
            ["Note"] = ""
        });

        var html = await AssertPartialAsync(response);
        Assert.Contains("Please enter a note.", html);
    }

    [Fact]
    public async Task NoteEditedToEmpty_ReturnsFormWithError_AndKeepsNote()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Submitted);
        var noteId = await _data.CreateNoteAsync(applicationId, TestUsers.Manager, "Original note.");
        var client = await LoginManagerAsync();

        var response = await client.PostFormAsync("/Applications/EditNote", new Dictionary<string, string>
        {
            ["ApplicationId"] = applicationId.ToString(),
            ["NoteId"] = noteId.ToString(),
            ["Note"] = "   "
        });

        var html = await AssertPartialAsync(response);
        Assert.Contains("Please enter a note.", html);
        Assert.Equal("Original note.", (await _data.GetNoteAsync(noteId))!.Note);
    }

    [Fact]
    public async Task ReviewModal_ForWithdrawnApplication_ShowsError()
    {
        var applicationId = await _data.CreateApplicationAsync(TestUsers.Applicant, ApplicationStatus.Withdrawn);
        var client = await LoginManagerAsync();

        var response = await client.GetAsync($"/Applications/Review/{applicationId}");

        var html = await AssertPartialAsync(response);
        Assert.Contains("This application is Withdrawn and can no longer be reviewed.", html);
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
