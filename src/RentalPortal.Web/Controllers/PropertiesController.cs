using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPortal.Web.Data;
using RentalPortal.Web.Domain;
using RentalPortal.Web.ViewModels;

namespace RentalPortal.Web.Controllers;

[Authorize(Roles = Roles.PropertyManager)]
public class PropertiesController(AppDbContext db) : Controller
{
    private const string FormView = "_PropertyForm";
    private const string ListView = "_PropertyList";

    /// <summary>
    /// Displays the property listings.
    /// </summary>
    /// <returns>A view containing the property rows.</returns>
    [HttpGet]
    public async Task<IActionResult> Index() => View(await GetPropertyRowsAsync());

    /// <summary>
    /// Displays the property list.
    /// </summary>
    /// <returns>A partial view containing the property rows.</returns>
    [HttpGet]
    public async Task<IActionResult> List() => PartialView(ListView, await GetPropertyRowsAsync());

    /// <summary>
    /// Displays the form for creating a property.
    /// </summary>
    /// <returns>A partial view containing a new property form view model.</returns>
    [HttpGet]
    public IActionResult Create() => PartialView(FormView, new PropertyFormViewModel());

    /// <summary>
    /// Creates a property from the submitted form and saves it.
    /// </summary>
    /// <param name="model">The property form values.</param>
    /// <returns>A partial form view when model validation fails; otherwise a JSON success response.</returns>
    [HttpPost]
    public async Task<IActionResult> Create(PropertyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return PartialView(FormView, model);
        }

        var property = new Property();
        model.ApplyTo(property);

        db.Properties.Add(property);
        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

    /// <summary>
    /// Displays the edit form for the specified property.
    /// </summary>
    /// <param name="id">The identifier of the property to edit.</param>
    /// <returns>The property edit form when the property exists; otherwise, a not-found result.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var property = await db.Properties.FindAsync(id);
        return property is null ? NotFound() : PartialView(FormView, PropertyFormViewModel.From(property));

    }

    /// <summary>
    /// Updates an existing property from the submitted property form.
    /// </summary>
    /// <param name="model">The property values to apply to the existing property.</param>
    /// <returns>A JSON success result after the property is updated, a partial form view when the model is invalid, or a
    /// not-found result when the property does not exist.</returns>
    [HttpPost]
    public async Task<IActionResult> Edit(PropertyFormViewModel model)
    {
        var property = await db.Properties.FindAsync(model.Id);

        if (property is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return PartialView(FormView, model);
        }

        model.ApplyTo(property);

        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

    /// <summary>
    /// Displays the confirmation view for deleting a property.
    /// </summary>
    /// <param name="id">The identifier of the property to delete.</param>
    /// <returns>A not-found result when the property does not exist; otherwise, a partial view containing the delete
    /// confirmation model.</returns>
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var property = await db.Properties.FindAsync(id);

        if (property is null)
        {
            return NotFound();
        }

        // Show the reason up front; _ConfirmDelete hides the Remove button when there's an error.
        return PartialView("_ConfirmDelete", BuildDeleteModel(property, await GetDeleteBlockerAsync(id)));
    }

    /// <summary>
    /// Deletes a property.
    /// </summary>
    /// <param name="id">The identifier of the property to delete.</param>
    /// <returns>A not-found result if the property does not exist; otherwise, a JSON result indicating success.</returns>
    [HttpPost, ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var property = await db.Properties.FindAsync(id);
        if (property is null)
        {
            return NotFound();
        }

        // Checked again on POST: an application may have been started since the modal opened.
        if (await GetDeleteBlockerAsync(id) is { } error)
        {
            return PartialView("_ConfirmDelete", BuildDeleteModel(property, error));
        }

        db.Properties.Remove(property);

        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

    public async Task<IActionResult> Details(int id)
    {
        var property = (await GetPropertyRowsAsync(id)).SingleOrDefault();
        return property is null ? NotFound() : View(property);
    }

    /// <summary>
    /// Checks whether a property can be removed. Applications are kept as records
    /// (RentalApplication -> Unit is DeleteBehavior.Restrict), so removing a property whose units
    /// have any application would fail in the database when the units cascade.
    /// </summary>
    /// <param name="propertyId">The identifier of the property to remove.</param>
    /// <returns>The message to show when the removal is blocked; otherwise, <c>null</c>.</returns>
    private async Task<string?> GetDeleteBlockerAsync(int propertyId) =>
        await db.RentalApplications.AnyAsync(a => a.Unit.PropertyId == propertyId)
            ? "This property has units with rental applications and can't be removed."
            : null;

    /// <summary>
    /// Creates the confirmation model for deleting a property.
    /// </summary>
    /// <param name="property">The property to remove.</param>
    /// <param name="error">An optional validation or processing error to display.</param>
    /// <returns>A confirmation model containing the deletion details.</returns>
    private ConfirmDeleteViewModel BuildDeleteModel(Property property, string? error = null) => new()
    {
        Title = "Remove Property",
        Message = $"Remove {property.Name} and all of its units?",
        ActionUrl = Url.Action(nameof(Delete), new { id = property.Id })!,
        Error = error
    };

    /// <summary>
    /// Retrieves property rows for display.
    /// </summary>
    /// <remarks>Each result includes the property address and unit count.</remarks>
    /// <param name="id">The property identifier used to filter results, or null to retrieve all properties.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the matching property rows ordered by
    /// name.</returns>
    private Task<List<PropertyRowViewModel>> GetPropertyRowsAsync(int? id = null)
    {
        return db.Properties
            .AsNoTracking()
            .Where(p => !id.HasValue || p.Id == id.Value)
            .OrderBy(p => p.Name)
            .Select(p => new PropertyRowViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Street + ", " + p.City + ", " + p.State + " " + p.PostalCode,
                UnitCount = p.Units.Count
            })
            .ToListAsync();
    }
}
