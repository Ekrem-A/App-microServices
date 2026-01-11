using Order.Application.Common.Interfaces;

namespace Order.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}

