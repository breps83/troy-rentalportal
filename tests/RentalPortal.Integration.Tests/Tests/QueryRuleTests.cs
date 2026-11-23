using System.Net;
using System.Text.RegularExpressions;
using RentalPortal.Integration.Tests.Infrastructure;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;

namespace RentalPortal.Integration.Tests.Tests;

/// <summary>
/// Some rules are repeated inline in EF queries because EF can't translate our C# methods to SQL.
/// These tests run the queries against the real database and check they agree with the domain method.
/// </summary>
[Collection(IntegrationCollection.Name)]
public partial class QueryRuleTests(IntegrationFixture fixture)
{
    private readonly IntegrationAppFactory _app = fixture.App;

    private readonly TestData _data = fixture.Data;

    [GeneratedRegex("<option[^>]* value=\"(\\d+)\"")]
    private static partial Regex OptionValueRegex();

    public static TheoryData<int, int> LeaseDates => new()
    {
        { -30, -1 },  // ended yesterday
        { -30, 0 },   // ends today
        { 0, 30 },    // starts today
        { 1, 30 },    // starts tomorrow
        { -30, 30 }   // in the middle
    };

    /// <summary>
    /// The listings query and StartApplicationAsync both repeat Lease.IsActive inline.
    /// </summary>
    [Theory]
    [MemberData(nameof(LeaseDates))]
    public async Task UnitIsAvailable_OnlyWhenNoLeaseIsActiveToday(int startOffset, int endOffset)
    {
        var today = _data.Today;
        var lease = new Lease { StartDate = today.AddDays(startOffset), EndDate = today.AddDays(endOffset) };
        var expectedAvailable = !lease.IsActive(today);

        var unit = await _data.CreateUnitAsync();
        await _data.AddLeaseAsync(unit.Id, lease.StartDate, lease.EndDate);

        var client = _app.CreateNoRedirectClient();
        await client.LoginAsync(TestUsers.Applicant);

        var listings = await client.GetStringAsync("/Listings");
        Assert.Equal(expectedAvailable, listings.Contains(WebUtility.HtmlEncode(unit.Property.Name)));

        var response = await client.PostFormAsync("/Applications/Apply", new Dictionary<string, string>
        {
            ["unitId"] = unit.Id.ToString()
        });

        var location = response.Headers.Location!.OriginalString;
        Assert.Equal(expectedAvailable, location.StartsWith("/Applications/Details/"));
    }

    /// <summary>
    /// UnitsController.GetUnitTypeOptionsAsync repeats UnitType.IsSelectableFor inline.
    /// </summary>
    [Fact]
    public async Task UnitTypeOptions_MatchIsSelectableFor()
    {
        var inactiveName = DbSeeder.UnitTypes.First(t => !t.IsActive).Name;
        var unit = await _data.CreateUnitAsync(inactiveName);
        var unitTypes = await _data.GetUnitTypesAsync();

        var client = _app.CreateNoRedirectClient();
        await client.LoginAsync(TestUsers.Manager);

        var createOptions = await GetOptionIdsAsync(client, $"/Units/Create?propertyId={unit.PropertyId}");
        var editOptions = await GetOptionIdsAsync(client, $"/Units/Edit/{unit.Id}");

        var expectedCreate = unitTypes.Where(t => t.IsSelectableFor(null)).Select(t => t.Id).Order();
        var expectedEdit = unitTypes.Where(t => t.IsSelectableFor(unit.UnitTypeId)).Select(t => t.Id).Order();

        Assert.Equal(expectedCreate, createOptions);
        Assert.Equal(expectedEdit, editOptions);
        Assert.Contains(unit.UnitTypeId, editOptions);
        Assert.DoesNotContain(unit.UnitTypeId, createOptions);
    }

    private static async Task<List<int>> GetOptionIdsAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);

        return OptionValueRegex().Matches(html)
            .Select(m => int.Parse(m.Groups[1].Value))
            .Order()
            .ToList();
    }
}
