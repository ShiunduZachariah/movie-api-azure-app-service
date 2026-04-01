namespace MovieApiAppService.Movies;

internal static class MovieRequestValidator
{
    public static Dictionary<string, string[]> Validate(CreateMovieRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        AddIfMissing(errors, nameof(CreateMovieRequest.Title), request.Title);
        AddIfMissing(errors, nameof(CreateMovieRequest.Genre), request.Genre);
        AddIfMissing(errors, nameof(CreateMovieRequest.Director), request.Director);

        if (request.ReleaseYear is < 1888 or > 2100)
        {
            Add(errors, nameof(CreateMovieRequest.ReleaseYear), "ReleaseYear must be between 1888 and 2100.");
        }

        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    private static void AddIfMissing(
        IDictionary<string, List<string>> errors,
        string fieldName,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, fieldName, $"{fieldName} is required.");
        }
    }

    private static void Add(
        IDictionary<string, List<string>> errors,
        string fieldName,
        string message)
    {
        if (!errors.TryGetValue(fieldName, out var messages))
        {
            messages = [];
            errors[fieldName] = messages;
        }

        messages.Add(message);
    }
}
