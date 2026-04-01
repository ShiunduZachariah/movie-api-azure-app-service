namespace movie_api_app_service.Tests.Integration;

public class MovieApiTests : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestWebApplicationFactory _factory;

    public MovieApiTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoot_ReturnsAppNameAndVersion()
    {
        var response = await _client.GetAsync("/");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AppInfoResponse>(TestJson.Default);

        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Name));
        Assert.NotNull(payload.Version);
        Assert.NotEmpty(payload.Version);
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenApiDocument_ReturnsConfiguredMetadata()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var info = document.RootElement.GetProperty("info");
        var paths = document.RootElement.GetProperty("paths");

        Assert.Equal("movie-api-app-service API", info.GetProperty("title").GetString());
        Assert.Equal("v1", info.GetProperty("version").GetString());
        Assert.True(paths.TryGetProperty("/api/movies", out _));
        Assert.True(paths.TryGetProperty("/health", out _));
    }

    [Fact]
    public async Task GetDocs_ReturnsScalarHtml()
    {
        var response = await _client.GetAsync("/docs");

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<!doctype html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Scalar", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSwagger_RedirectsToDocs()
    {
        using var redirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await redirectClient.GetAsync("/swagger");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/docs", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task GetMovies_ReturnsSeededCollection()
    {
        var response = await _client.GetAsync("/api/movies");

        response.EnsureSuccessStatusCode();

        var movies = await response.Content.ReadFromJsonAsync<List<MovieResponse>>(TestJson.Default);

        Assert.NotNull(movies);
        Assert.NotEmpty(movies!);
    }

    [Fact]
    public async Task GetMovieById_ReturnsMovie()
    {
        var moviesResponse = await _client.GetAsync("/api/movies");
        var movies = await moviesResponse.Content.ReadFromJsonAsync<List<MovieResponse>>(TestJson.Default);
        Assert.NotNull(movies);
        Assert.NotEmpty(movies!);
        var movie = movies[0];

        var response = await _client.GetAsync($"/api/movies/{movie.Id}");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<MovieResponse>(TestJson.Default);

        Assert.NotNull(payload);
        Assert.Equal(movie.Id, payload!.Id);
        Assert.Equal(movie.Title, payload.Title);
    }

    [Fact]
    public async Task GetMovieById_WhenMovieDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/movies/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostMovie_CreatesMovie()
    {
        var request = new CreateMovieRequest("The Test Movie", 2026, "Drama", "Test Director");

        var response = await _client.PostAsJsonAsync("/api/movies", request, TestJson.Default);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MovieResponse>(TestJson.Default);

        Assert.NotNull(payload);
        Assert.Equal(request.Title, payload!.Title);
        Assert.Equal(request.ReleaseYear, payload.ReleaseYear);
        Assert.Equal(request.Genre, payload.Genre);
        Assert.Equal(request.Director, payload.Director);
    }

    [Fact]
    public async Task PostMovie_WhenRequestIsInvalid_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/movies", new
        {
            title = " ",
            releaseYear = 1800,
            genre = "",
            director = "",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = document.RootElement.GetProperty("errors");

        Assert.True(errors.TryGetProperty("Title", out _));
        Assert.True(errors.TryGetProperty("ReleaseYear", out _));
        Assert.True(errors.TryGetProperty("Genre", out _));
        Assert.True(errors.TryGetProperty("Director", out _));
    }

    [Fact]
    public async Task GetDocs_InProductionWithoutOverride_ReturnsNotFound()
    {
        using var productionFactory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var productionClient = productionFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await productionClient.GetAsync("/docs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record AppInfoResponse(string Name, string Version);
    private sealed record MovieResponse(int Id, string Title, int ReleaseYear, string Genre, string Director);
    private sealed record CreateMovieRequest(string Title, int ReleaseYear, string Genre, string Director);
}
