using RentalPortal.Web.Domain;
using RentalPortal.Web.Enums;
using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.ViewModels.Applications;

public class ReviewViewModel : IValidatableObject
{
    public int ApplicationId { get; set; }

    public bool CanReview { get; set; } = true;

    [Required(ErrorMessage = "Please set the review status.")]
    public ApplicationReviewStatus? ReviewStatus { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    [StringLength(ApplicationNote.MaxLength)]
    [Display(Name = "Internal note")]
    public string? InternalNote { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (ReviewStatus is ApplicationReviewStatus.Return
            or ApplicationReviewStatus.Deny
            && string.IsNullOrWhiteSpace(Comment))
            yield return new ValidationResult("A comment is required!", [nameof(Comment)]);

    }

}
