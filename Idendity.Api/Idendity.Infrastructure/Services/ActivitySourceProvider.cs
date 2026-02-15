using System.Diagnostics;

namespace Idendity.Infrastructure.Services;

public static class ActivitySourceProvider
{
    public static ActivitySource Source { get; set; } = null!;
}
