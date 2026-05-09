using Microsoft.Extensions.DependencyInjection;

namespace TestHub.Services;

/// <summary>
/// Tiny accessor over the MAUI service provider. Used by Shell-resolved
/// pages whose constructors are parameterless.
/// </summary>
public static class ServiceHelper
{
    public static T? GetService<T>() where T : class
        => IPlatformApplication.Current?.Services.GetService<T>();

    public static T GetRequiredService<T>() where T : notnull
    {
        var sp = IPlatformApplication.Current?.Services
                 ?? throw new InvalidOperationException("Service provider is not initialized.");
        return sp.GetRequiredService<T>();
    }
}
