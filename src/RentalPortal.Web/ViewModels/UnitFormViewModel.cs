using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace RentalPortal.Web.ViewModels;

public class UnitFormViewModel
{

    public int Id { get; set; }
    
    public int PropertyId { get; set; }

    [Required, StringLength(20), Display(Name = "Unit number")]
    public string UnitNumber { get; set; } = string.Empty;

    [Required, Range(0,10)]
    public int? Bedrooms { get; set; }

    // Range(typeof(decimal), ...) compares as decimal. The Range(int, int) overload converts the value
    // to int first, so an amount over int.MaxValue throws OverflowException (a 500) instead of failing validation.
    [Required, Range(typeof(decimal), "1", "100000", ErrorMessage = "Monthly rent must be between $1 and $100,000.")]
    [DataType(DataType.Currency), Display(Name = "Monthly rent")]
    public decimal MonthlyRent { get; set; }

    // Nullable so [Required] can fire: a non-nullable int always has a value (0 when nothing is selected).
    [Required, Display(Name = "Unit type")]
    public int? UnitTypeId { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> UnitTypes { get; set; } = new List<SelectListItem>();



}
