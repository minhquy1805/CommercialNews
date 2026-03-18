using Identity.Application.Contracts.Ports;

namespace Identity.Infrastructure.Time
{
    public sealed class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
