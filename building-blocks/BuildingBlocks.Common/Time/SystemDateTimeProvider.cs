using BuildingBlocks.Common.Abstractions;

namespace BuildingBlocks.Common.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
