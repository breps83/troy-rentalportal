using RentalPortal.Web.Enums;
using RentalPortal.Web.Extensions;

namespace RentalPortal.Tests;

/// <summary>
/// Tests for moving between application sections with Continue and Back (FR 3.b.i, FR 3.b.ii):
/// Applicant Information, then Residence History, then the Summary.
/// </summary>
public class ApplicationSectionExtensionsTests
{
    // ---- NextSection (Continue) ----

    [Theory]
    [InlineData(ApplicationSection.Info, ApplicationSection.ResidenceHistory)]
    [InlineData(ApplicationSection.ResidenceHistory, ApplicationSection.Summary)]
    public void NextSection_MovesToTheFollowingSection(ApplicationSection section, ApplicationSection expected)
    {
        // Act
        var next = section.NextSection();

        // Assert
        Assert.Equal(expected, next);
    }

    /// <summary>The Summary is the last section, so Continue stays there.</summary>
    [Fact]
    public void NextSection_FromSummary_StaysOnSummary()
    {
        // Act
        var next = ApplicationSection.Summary.NextSection();

        // Assert
        Assert.Equal(ApplicationSection.Summary, next);
    }

    // ---- PreviousSection (Back) ----

    [Theory]
    [InlineData(ApplicationSection.Summary, ApplicationSection.ResidenceHistory)]
    [InlineData(ApplicationSection.ResidenceHistory, ApplicationSection.Info)]
    public void PreviousSection_MovesToTheEarlierSection(ApplicationSection section, ApplicationSection expected)
    {
        // Act
        var previous = section.PreviousSection();

        // Assert
        Assert.Equal(expected, previous);
    }

    /// <summary>Applicant Information is the first section, so Back stays there.</summary>
    [Fact]
    public void PreviousSection_FromInfo_StaysOnInfo()
    {
        // Act
        var previous = ApplicationSection.Info.PreviousSection();

        // Assert
        Assert.Equal(ApplicationSection.Info, previous);
    }

    // ---- Title ----

    [Theory]
    [InlineData(ApplicationSection.Info, "Applicant Information")]
    [InlineData(ApplicationSection.ResidenceHistory, "Residence History")]
    [InlineData(ApplicationSection.Summary, "Summary")]
    public void Title_IsTheSectionHeading(ApplicationSection section, string expected)
    {
        // Act
        var title = section.Title();

        // Assert
        Assert.Equal(expected, title);
    }
}
