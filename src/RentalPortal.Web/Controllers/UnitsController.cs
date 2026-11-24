using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.ViewModels.Shared;
using RentalPortal.Web.ViewModels.Units;

namespace RentalPortal.Web.Controllers;

/// <summary>
/// Manages rental units for property managers.
/// </summary>
/// <remarks>Accessible only to property manager users.</remarks>
/// <param name="db">The application database context.</param>
[Authorize(Roles = Roles.PropertyManager)]
public class UnitsController(AppDbContext db) : Controller
{
    /// <summary>
    /// Displays the units associated with the specified property.
    /// </summary>
    /// <param name="propertyId">The identifier of the property whose units are displayed.</param>
    /// <returns>A view component result that renders the unit list.</returns>
    [HttpGet]
    public IActionResult List(int propertyId) => 
        ViewComponent("UnitList", new { propertyId });

    /// <summary>
    /// Displays the form for creating a unit for the specified property.
    /// </summary>
    /// <param name="propertyId">The identifier of the property to which the unit will belong.</param>
    /// <returns>The unit creation partial view, or a not-found result when the property does not exist.</returns>
    [HttpGet]
    public async Task<IActionResult> Create(int propertyId)
    {
        if (!await db.Properties.AnyAsync(p => p.Id == propertyId))
            return NotFound();

        var model = new UnitFormViewModel()
        { 
            PropertyId = propertyId,
            UnitTypes = await GetUnitTypeOptionsAsync(null)
        };

        return PartialView("_UnitForm", model);
    }

    /// <summary>
    /// Creates a unit for an existing property.
    /// </summary>
    /// <param name="model">The unit values to validate and add.</param>
    /// <returns>A not-found result when the property does not exist; a partial view containing validation errors or a JSON
    /// result indicating success.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(UnitFormViewModel model)
    {
        if (!await db.Properties.AnyAsync(p => p.Id == model.PropertyId))
            return NotFound();

        await ValidateUnitAsync(model, currentUnitTypeId: null);

        if (!ModelState.IsValid)
        {
            model.UnitTypes = await GetUnitTypeOptionsAsync(null);

            return PartialView("_UnitForm", model);
        }

        db.Units.Add(new Unit
        {
            PropertyId = model.PropertyId,
            UnitNumber = model.UnitNumber.Trim(),
            Bedrooms = model.Bedrooms!.Value,
            MonthlyRent = model.MonthlyRent,
            UnitTypeId = model.UnitTypeId!.Value
        });

        await db.SaveChangesAsync();

        return Json(new { success = true });

    }

    /// <summary>
    /// Displays the edit form for the specified unit.
    /// </summary>
    /// <param name="id">The unique identifier of the unit.</param>
    /// <returns>The edit form partial view when the unit exists; otherwise, a not-found result.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var unit = await db.Units.FindAsync(id);

        if (unit is null)
            return NotFound();

        var model = new UnitFormViewModel
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            UnitNumber = unit.UnitNumber,
            Bedrooms = unit.Bedrooms,
            MonthlyRent = unit.MonthlyRent,
            UnitTypeId = unit.UnitTypeId,
            UnitTypes = await GetUnitTypeOptionsAsync(unit.UnitTypeId)
        };

        return PartialView("_UnitForm", model);
    }

    /// <summary>
    /// Updates an existing rental unit with validated form values.
    /// </summary>
    /// <remarks>The unit's stored property association is preserved even if the posted property identifier is
    /// stale or altered.</remarks>
    /// <param name="model">The unit values to apply.</param>
    /// <returns>A not-found result if the unit does not exist, a partial view containing validation errors, or a success result.</returns>
    [HttpPost]
    public async Task<IActionResult> Edit(UnitFormViewModel model)
    {
        var unit = await db.Units.FindAsync(model.Id);

        if (unit is null)
            return NotFound();

        // Use the stored property, not the posted hidden field, so a stale or tampered value
        // can't point the duplicate unit-number check at a different property.
        model.PropertyId = unit.PropertyId;

        await ValidateUnitAsync(model, currentUnitTypeId: unit.UnitTypeId);

        if (!ModelState.IsValid)
        {
            model.UnitTypes = await GetUnitTypeOptionsAsync(unit.UnitTypeId);

            return PartialView("_UnitForm", model);
        }

        unit.UnitNumber = model.UnitNumber.Trim();

        unit.Bedrooms = model.Bedrooms!.Value;

        unit.MonthlyRent = model.MonthlyRent;

        unit.UnitTypeId = model.UnitTypeId!.Value;

        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

    /// <summary>
    /// Displays a confirmation view for deleting the specified unit.
    /// </summary>
    /// <param name="id">The unit identifier.</param>
    /// <returns>A not-found result when the unit does not exist; otherwise, a partial delete confirmation view.</returns>
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var unit = await db.Units.FindAsync(id);

        if (unit is null)
        {
            return NotFound();
        }

        return PartialView("_ConfirmDelete", 
            BuildDeleteModel(unit, await GetDeleteBlockerAsync(id)));
    }

    /// <summary>
    /// Deletes a unit after confirming it has no blocking applications or leases.
    /// </summary>
    /// <remarks>Rechecks delete blockers at submission time because an application or lease may have been
    /// created while the confirmation was open.</remarks>
    /// <param name="id">The ID of the unit to delete.</param>
    /// <returns>The delete confirmation partial view when the unit is missing or blocked, or a success response after deletion.</returns>
    [HttpPost, ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var unit = await db.Units.FindAsync(id);

        if (unit is null)
            return NotFound();

        // Checked again on POST: an application may have been started since the modal opened.
        if (await GetDeleteBlockerAsync(id) is { } error)
            return PartialView("_ConfirmDelete", BuildDeleteModel(unit, error));

        db.Units.Remove(unit);

        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

    private async Task<IReadOnlyList<SelectListItem>> GetUnitTypeOptionsAsync(int? currentUnitTypeId)
    {
        // Note: this condition intentionally duplicates UnitType.IsSelectableFor. EF Core translates
        // the lambda to SQL, and it can't translate calls to our own C# methods, so
        // .Where(t => t.IsSelectableFor(...)) throws InvalidOperationException at runtime (the build
        // doesn't catch it). Keep the two in sync if the rule changes.
        return await db.UnitTypes
            .AsNoTracking()
            .Where(t => t.IsActive || t.Id == currentUnitTypeId)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem(t.IsActive ? t.Name : t.Name + " (inactive)", t.Id.ToString()))
            .ToListAsync();
    }

    private async Task ValidateUnitAsync(UnitFormViewModel model, int? currentUnitTypeId)
    {
        if (model.UnitTypeId is { } unitTypeId)
        {
            var unitType = await db.UnitTypes.FindAsync(unitTypeId);

            if (unitType is null || !unitType.IsSelectableFor(currentUnitTypeId))
            {
                ModelState.AddModelError(nameof(model.UnitTypeId), "This unit type is inactive and can't be selected.");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.UnitNumber))
        {
            var unitNumber = model.UnitNumber.Trim();

            var duplicate = await db.Units.AnyAsync(u =>
                u.PropertyId == model.PropertyId && u.UnitNumber == unitNumber && u.Id != model.Id);

            if (duplicate)
                ModelState.AddModelError(nameof(model.UnitNumber), "This property already has a unit with that number.");
        }
    }

    // Applications are kept as records (RentalApplication -> Unit is DeleteBehavior.Restrict), so a unit
    // with any application can't be deleted. Returns the message to show, or null when the delete is allowed.
    private async Task<string?> GetDeleteBlockerAsync(int unitId) =>
        await db.RentalApplications.AnyAsync(a => a.UnitId == unitId)
            ? "This unit has rental applications and can't be deleted."
            : null;

    private ConfirmDeleteViewModel BuildDeleteModel(Unit unit, string? error = null) => new()
    {
        Title = "Delete unit",
        Message = $"Delete unit {unit.UnitNumber}?",
        ActionUrl = Url.Action(nameof(Delete), new { id = unit.Id })!,
        Error = error
    };

}

