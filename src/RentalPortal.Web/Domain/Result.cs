namespace RentalPortal.Web.Domain;

public record Result(bool Succeeded, string? Error = null)
{
    public static Result Success() => new(true);

    public static Result Failure(string error) => new(false, error);
}
