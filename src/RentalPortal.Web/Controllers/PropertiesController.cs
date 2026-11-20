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

    public async Task<IActionResult> Index()
    {
        var properties = await db.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PropertyRowViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = $"{p.Street}, {p.City}, {p.State} {p.PostalCode}"
            })
            .ToListAsync();

        return View(properties);
    }

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

        return RedirectToAction(nameof(Index));
    }
}
