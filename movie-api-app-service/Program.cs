using MovieApiAppService.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMovieApi();

var app = builder.Build();

app.UseMovieApi();

app.Run();

public partial class Program
{
}
