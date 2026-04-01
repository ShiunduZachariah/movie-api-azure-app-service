using MovieApiAppService.Movies;
using Scalar.AspNetCore;

namespace MovieApiAppService.Api;

public static class MovieApiApplicationExtensions
{
    public static WebApplication UseMovieApi(this WebApplication app)
    {
        app.UseExceptionHandler();

        var enableApiDocs = !app.Environment.IsProduction() ||
            app.Configuration.GetValue<bool>("ApiDocumentation:EnableInProduction") ||
            app.Configuration.GetValue<bool>("Swagger:EnableInProduction");

        if (enableApiDocs)
        {
            app.MapOpenApi();
            app.MapScalarApiReference("/docs");
            app.MapGet("/swagger", () => Results.Redirect("/docs"))
                .ExcludeFromDescription();
        }

        app.MapGet("/", () => Results.Ok(AppInfoResponse.FromCurrentAssembly()))
            .WithName("GetAppInfo")
            .WithTags("Metadata")
            .WithSummary("Get application metadata")
            .WithDescription("Returns the application name and current service version.");

        app.MapGet("/version", () => Results.Ok(new VersionResponse(AppInfo.Version)))
            .WithName("GetVersion")
            .WithTags("Metadata")
            .WithSummary("Get the current service version")
            .WithDescription("Returns the current service build version.");

        app.MapGet("/health", () => Results.Ok(new HealthResponse("healthy")))
            .WithName("GetHealth")
            .WithTags("Health")
            .WithSummary("Check service health")
            .WithDescription("Returns a simple health payload when the service is running.");

        var movies = app.MapGroup("/api/movies")
            .WithTags("Movies");

        movies.MapGet(string.Empty, (IMovieCatalog catalog) => Results.Ok(catalog.GetAll()))
            .WithName("GetMovies")
            .WithSummary("List all movies")
            .WithDescription("Returns the current in-memory movie catalog.");

        movies.MapGet("/{id:int}", (int id, IMovieCatalog catalog) =>
        {
            var movie = catalog.GetById(id);
            return movie is null ? Results.NotFound() : Results.Ok(movie);
        })
            .WithName("GetMovieById")
            .WithSummary("Get a movie by id")
            .WithDescription("Returns a single movie when the requested id exists.");

        movies.MapPost(string.Empty, (CreateMovieRequest request, IMovieCatalog catalog) =>
        {
            var validationErrors = MovieRequestValidator.Validate(request);
            if (validationErrors.Count > 0)
            {
                return Results.ValidationProblem(validationErrors);
            }

            var created = catalog.Create(request);
            return Results.Created($"/api/movies/{created.Id}", created);
        })
            .WithName("CreateMovie")
            .WithSummary("Create a movie")
            .WithDescription("Creates a movie in the in-memory catalog when the request is valid.");

        return app;
    }
}
