using RentalPortal.Web.Enums;

namespace RentalPortal.Web.Extensions;

public static class ApplicationSectionExtensions
{
    /// <summary>
    /// Gets the application section that follows the specified section.
    /// </summary>
    /// <param name="section">The current application section.</param>
    /// <returns>The following application section, or <see cref="ApplicationSection.Summary"/> if the current section is already
    /// <see cref="ApplicationSection.Summary"/>.</returns>
    public static ApplicationSection NextSection(this ApplicationSection section) =>
        section == ApplicationSection.Summary ? section : section + 1;

    /// <summary>
    /// Gets the application section preceding the specified section.
    /// </summary>
    /// <param name="section">The application section for which to find the previous section.</param>
    /// <returns>The preceding section, or the Info section when section is already Info.</returns>
    public static ApplicationSection PreviousSection(this ApplicationSection section) =>
        section == ApplicationSection.Info ? section : section - 1;

    /// <summary>
    /// Gets the display title for an application section.
    /// </summary>
    /// <param name="section">The application section.</param>
    /// <returns>The display title for the application section.</returns>
    public static string Title(this ApplicationSection section) => section switch
    {
        ApplicationSection.Info => "Applicant Information",
        ApplicationSection.ResidenceHistory => "Residence History",
        _ => "Summary"
    };

}
