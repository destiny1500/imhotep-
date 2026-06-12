using Imhotep.Application.Common.Interfaces;

namespace Imhotep.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
