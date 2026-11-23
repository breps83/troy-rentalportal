namespace RentalPortal.Web.ViewModels.Shared;

public class ErrorViewModel
{
    public int? StatusCode { get; set; }

    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}