using System.Reflection;

namespace MovieApiAppService.Api;

public sealed record AppInfoResponse(string Name, string Version)
{
    public static AppInfoResponse FromCurrentAssembly()
    {
        return new AppInfoResponse(AppInfo.Name, AppInfo.Version);
    }
}

public sealed record VersionResponse(string Version);

public sealed record HealthResponse(string Status);

public static class AppInfo
{
    private static Assembly AppAssembly => typeof(Program).Assembly;

    public static string Name => AppAssembly.GetName().Name ?? "movie-api-app-service";

    public static string Version
    {
        get
        {
            var informationalVersion = AppAssembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                var plusIndex = informationalVersion.IndexOf('+');
                return plusIndex >= 0 ? informationalVersion[..plusIndex] : informationalVersion;
            }

            return AppAssembly.GetName().Version?.ToString() ?? "0.0.0";
        }
    }
}
