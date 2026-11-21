namespace RentalPortal.Web.Domain;

public class Residence
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }

    public string Address { get; set; } = string.Empty;

    public string LandlordName { get; set; } = string.Empty;

    public string LandlordPhone { get; set; } = string.Empty;

    public DateOnly MoveInDate { get; set; }

    public DateOnly MoveOutDate { get; set; }
}
