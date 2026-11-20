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

    [HttpGet]
    public async Task<IActionResult> Index() => View(await GetPropertyRowsAsync());

    [HttpGet]
    public async Task<IActionResult> List() => PartialView(ListView, await GetPropertyRowsAsync());

    [HttpGet]
    public IActionResult Create() => PartialView(FormView, new PropertyFormViewModel());

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
