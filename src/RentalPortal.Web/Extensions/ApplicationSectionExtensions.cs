using RentalPortal.Web.Enums;

namespace RentalPortal.Web.Extensions;

public static class ApplicationSectionExtensions
{
    public static ApplicationSection NextSection(this ApplicationSection section) =>
        section == ApplicationSection.Summary ? section : section + 1;

    public static ApplicationSection PreviousSection(this ApplicationSection section) =>
        section == ApplicationSection.Info ? section : section - 1;

    public static string Title(this ApplicationSection section) => section switch
    {
        ApplicationSection.Info => "Applicant Information",
        ApplicationSection.ResidenceHistory => "Residence History",
        _ => "Summary"
    };
}
