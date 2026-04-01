namespace MovieApiAppService.Movies;

public interface IMovieCatalog
{
    IReadOnlyList<Movie> GetAll();

    Movie? GetById(int id);

    Movie Create(CreateMovieRequest request);
}
