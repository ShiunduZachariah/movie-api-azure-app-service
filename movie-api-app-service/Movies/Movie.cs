namespace MovieApiAppService.Movies;

public sealed record Movie
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public int ReleaseYear { get; init; }

    public string Genre { get; init; } = string.Empty;

    public string Director { get; init; } = string.Empty;
}
