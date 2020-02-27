using Pixie.Core.Exceptions;
using Pixie.Core.Services;
using Pixie.Core.Services.Internal;

public static class PXEnvironmentExtensions
{
    public static string GetString(this IPXEnvironmentService service, string key, string defaultValue = null) {
        return service.GetString(key, delegate { return defaultValue; });
    }

    public static int GetInt(this IPXEnvironmentService service, string key, int defaultValue = 0) {
        return service.GetInt(key, delegate { return defaultValue; });
    }
}