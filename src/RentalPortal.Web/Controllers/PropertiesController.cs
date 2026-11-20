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

        if(property is null)
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

        return property is null ? NotFound() : PartialView("_ConfirmDelete", BuildDeleteModel(property));
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

        db.Properties.Remove(property);

        await db.SaveChangesAsync();

        return Json(new { success = true });
    }

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
    /// Retrieves properties and creates the rows used for property display.
    /// </summary>
    /// <returns>A task whose result contains the property rows ordered by property name.</returns>
    private Task<List<PropertyRowViewModel>> GetPropertyRowsAsync()
    {
        return db.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PropertyRowViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = $"{p.Street}, {p.City}, {p.State} {p.PostalCode}"
            })
            .ToListAsync();
    }
}
