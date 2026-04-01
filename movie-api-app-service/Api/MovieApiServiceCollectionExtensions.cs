using MovieApiAppService.Movies;
using Microsoft.Extensions.DependencyInjection;

namespace MovieApiAppService.Api;

public static class MovieApiServiceCollectionExtensions
{
    public static IServiceCollection AddMovieApi(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, _) =>
            {
                document.Info ??= new();
                document.Info.Title = $"{AppInfo.Name} API";
                document.Info.Version = context.DocumentName;
                document.Info.Description = $"HTTP API for app metadata, health, and movie catalog operations. Service build: {AppInfo.Version}.";

                return Task.CompletedTask;
            });
        });
        services.AddSingleton<IMovieCatalog, InMemoryMovieCatalog>();
        return services;
    }
}
