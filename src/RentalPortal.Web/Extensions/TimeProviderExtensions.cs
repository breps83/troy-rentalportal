using System.Runtime.CompilerServices;

namespace RentalPortal.Web.Extensions;

public static class TimeProviderExtensions
{
    public static DateOnly GetToday(this TimeProvider tp) =>
        DateOnly.FromDateTime(tp.GetLocalNow().DateTime);
}
