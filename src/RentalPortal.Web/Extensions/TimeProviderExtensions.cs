namespace RentalPortal.Web.Extensions;

public static class TimeProviderExtensions
{
    /// <summary>
    /// Gets the current local date.
    /// </summary>
    /// <param name="tp">The time provider used to obtain the current local date and time.</param>
    /// <returns>The current local date.</returns>
    public static DateOnly GetToday(this TimeProvider tp) =>
        DateOnly.FromDateTime(tp.GetLocalNow().DateTime);
}
