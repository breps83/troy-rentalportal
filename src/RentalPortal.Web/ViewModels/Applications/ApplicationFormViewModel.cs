using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RentalPortal.Web.Enums;

namespace RentalPortal.Web.ViewModels.Applications;

public class ApplicationFormViewModel
{

    public const string Continue = "continue";
    
    public const string Back = "back";

    public const string Submit = "submit";

    public int Id { get; set; }

    [ValidateNever]
    public ApplicationStatus Status { get; set; }

    [ValidateNever]
    public string PropertyName { get; set; } = string.Empty;

    [ValidateNever]
    public string UnitNumber { get; set; } = string.Empty;

    [ValidateNever]
    public decimal MonthlyRent { get; set; }

    [ValidateNever]
    public bool IsEditable { get; set; }

    [ValidateNever] 
    public bool CanSubmit { get; set; }

    [ValidateNever]
    public bool ApplicantInfoCompleted { get; set; }

    [ValidateNever]
    public bool ResidenceHistoryCompleted { get; set; }

    public ApplicationSection CurrentSection { get; set; }

    public ApplicantInfoViewModel ApplicantInfo { get; set; } = new();

    public bool IsSectionEditable(ApplicationSection section) => IsEditable && CurrentSection == section;

    public bool IsSectionCompleted(ApplicationSection section) => section switch
    {
        ApplicationSection.Info => ApplicantInfoCompleted,
        ApplicationSection.ResidenceHistory => ResidenceHistoryCompleted,
        _ => false
    };

}
