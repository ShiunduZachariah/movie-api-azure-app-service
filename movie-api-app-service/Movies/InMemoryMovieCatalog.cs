using System.Text.Json;

namespace MovieApiAppService.Movies;

public sealed class InMemoryMovieCatalog : IMovieCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object _gate = new();
    private readonly List<Movie> _movies;
    private int _nextId;

    public InMemoryMovieCatalog()
    {
        _movies = LoadSeedMovies().ToList();
        _nextId = _movies.Count == 0 ? 1 : _movies.Max(movie => movie.Id) + 1;
    }

    public IReadOnlyList<Movie> GetAll()
    {
        lock (_gate)
        {
            return _movies
                .OrderBy(movie => movie.Id)
                .ToArray();
        }
    }

    public Movie? GetById(int id)
    {
        lock (_gate)
        {
            return _movies.FirstOrDefault(movie => movie.Id == id);
        }
    }

    public Movie Create(CreateMovieRequest request)
    {
        lock (_gate)
        {
            var movie = new Movie
            {
                Id = _nextId++,
                Title = request.Title.Trim(),
                ReleaseYear = request.ReleaseYear,
                Genre = request.Genre.Trim(),
                Director = request.Director.Trim(),
            };

            _movies.Add(movie);
            return movie;
        }
    }

    private static IEnumerable<Movie> LoadSeedMovies()
    {
        foreach (var candidatePath in GetCandidateSeedPaths())
        {
            if (!File.Exists(candidatePath))
            {
                continue;
            }

            var json = File.ReadAllText(candidatePath);
            var movies = JsonSerializer.Deserialize<List<Movie>>(json, JsonOptions);
            if (movies is { Count: > 0 })
            {
                return movies;
            }
        }

        var fallbackMovies = JsonSerializer.Deserialize<List<Movie>>(SeedJson, JsonOptions);
        return fallbackMovies ?? [];
    }

    private static IEnumerable<string> GetCandidateSeedPaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Movies", "Seed", "movies.seed.json");
        yield return Path.Combine(AppContext.BaseDirectory, "Seed", "movies.seed.json");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "Movies", "Seed", "movies.seed.json");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "Seed", "movies.seed.json");
    }

    private const string SeedJson = """
[
  {
    "id": 1,
    "title": "Arrival",
    "releaseYear": 2016,
    "genre": "Science Fiction",
    "director": "Denis Villeneuve"
  },
  {
    "id": 2,
    "title": "The Shawshank Redemption",
    "releaseYear": 1994,
    "genre": "Drama",
    "director": "Frank Darabont"
  },
  {
    "id": 3,
    "title": "The Grand Budapest Hotel",
    "releaseYear": 2014,
    "genre": "Comedy Drama",
    "director": "Wes Anderson"
  },
  {
    "id": 4,
    "title": "Mad Max: Fury Road",
    "releaseYear": 2015,
    "genre": "Action",
    "director": "George Miller"
  },
  {
    "id": 5,
    "title": "The Iron Giant",
    "releaseYear": 1999,
    "genre": "Animation",
    "director": "Brad Bird"
  }
]
""";
}
