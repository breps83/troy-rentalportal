namespace RentalPortal.Web.Domain;

public class Residence
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }

    public string Street { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Address => $"{Street}, {City}, {State} {PostalCode}";

    public string LandlordName { get; set; } = string.Empty;

    public string LandlordPhone { get; set; } = string.Empty;

    public DateOnly MoveInDate { get; set; }

    public DateOnly MoveOutDate { get; set; }
}
