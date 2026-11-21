using Microsoft.AspNetCore.Mvc;
using RentalPortal.Web.ViewModels;
using System.Reflection.Metadata.Ecma335;

namespace RentalPortal.Web.ViewComponents;

public class ResidenceHistoryViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int applicationId, bool editable)
    {
        return View(new ResidenceHistoryViewModel());
    }
}
