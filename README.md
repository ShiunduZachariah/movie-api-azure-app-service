# Movie API App Service

This repository contains a small ASP.NET Core Minimal API used to test creating and validating an Azure App Service deployment.

The goal of the project is not to model a full production movie platform. It is intentionally simple so it can be used to verify the full flow of:

- creating an ASP.NET Minimal API
- exposing a few predictable endpoints
- generating OpenAPI documentation
- serving interactive API docs with Scalar
- running integration tests
- deploying the app to Azure App Service and confirming it works

Because of that, this project is a good smoke-test application for Azure App Service scenarios.

## Purpose

This app exists to test creating an Azure App Service using ASP.NET Minimal APIs.

It gives you a lightweight HTTP service with:

- metadata endpoints for app identity and version checks
- a health endpoint for deployment validation
- an in-memory movie catalog for simple CRUD-style testing
- OpenAPI output for tooling and contract inspection
- Scalar API documentation for browsing and manual verification

If you deploy this app to Azure App Service, you can use `/`, `/version`, `/health`, `/api/movies`, and `/docs` to quickly confirm the deployment is healthy and serving requests correctly.

## Tech Stack

- ASP.NET Core Minimal APIs
- .NET 10
- Built-in ASP.NET Core OpenAPI generation
- Scalar for API documentation UI
- xUnit integration tests with `WebApplicationFactory`

## What The API Does

The API exposes a small in-memory movie catalog. Data is seeded from `movie-api-app-service/Movies/Seed/movies.seed.json` and stored in memory at runtime, so changes are not persisted between app restarts.

This makes it useful for App Service testing because:

- startup is fast
- there is no database dependency
- requests are deterministic
- health and routing are easy to verify

## Endpoints

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/` | Returns application name and version |
| `GET` | `/version` | Returns only the current version |
| `GET` | `/health` | Returns a simple healthy status |
| `GET` | `/api/movies` | Returns all seeded and created movies |
| `GET` | `/api/movies/{id}` | Returns a movie by id or `404` |
| `POST` | `/api/movies` | Creates a movie in the in-memory catalog |
| `GET` | `/openapi/v1.json` | Returns the generated OpenAPI document |
| `GET` | `/docs` | Returns the Scalar API documentation UI |
| `GET` | `/swagger` | Redirects to `/docs` for compatibility |

## Example Responses

### `GET /`

```json
{
  "name": "movie-api-app-service",
  "version": "1.0.0"
}
```

### `GET /health`

```json
{
  "status": "healthy"
}
```

### `POST /api/movies`

Request:

```json
{
  "title": "The Matrix",
  "releaseYear": 1999,
  "genre": "Science Fiction",
  "director": "Lana Wachowski and Lilly Wachowski"
}
```

Successful response:

```json
{
  "id": 6,
  "title": "The Matrix",
  "releaseYear": 1999,
  "genre": "Science Fiction",
  "director": "Lana Wachowski and Lilly Wachowski"
}
```

Validation failure example:

```json
{
  "errors": {
    "Title": [
      "Title is required."
    ],
    "ReleaseYear": [
      "ReleaseYear must be between 1888 and 2100."
    ]
  }
}
```

## API Documentation

This project uses two documentation outputs:

- OpenAPI JSON at `/openapi/v1.json`
- Scalar UI at `/docs`

In non-production environments, the Scalar docs are enabled by default.

In production, docs are disabled by default and can be enabled with configuration:

```json
{
  "ApiDocumentation": {
    "EnableInProduction": true
  }
}
```

`/swagger` is kept as a redirect to `/docs` so older bookmarks or habits still work.

## Running Locally

Prerequisite:

- .NET 10 SDK installed

From the repository root:

```powershell
dotnet run --project .\movie-api-app-service\
```

Default local URLs are defined in `movie-api-app-service/Properties/launchSettings.json`.

Once the app is running, useful URLs are:

- `http://localhost:5275/`
- `http://localhost:5275/health`
- `http://localhost:5275/api/movies`
- `http://localhost:5275/openapi/v1.json`
- `http://localhost:5275/docs`

You can also use the request file at `movie-api-app-service/movie-api-app-service.http`.

## Running Tests

From the repository root:

```powershell
dotnet test .\tests\movie-api-app-service.Tests\movie-api-app-service.Tests.csproj
```

The integration tests verify:

- root metadata endpoint
- health endpoint
- OpenAPI document availability
- Scalar docs endpoint
- `/swagger` redirect behavior
- seeded movie retrieval
- fetch-by-id behavior
- `404` handling for missing movies
- successful movie creation
- validation failures for invalid payloads
- docs disabled by default in production

## Deploy To Azure App Service With Azure CLI

This project is intended to be a simple Azure App Service test application for ASP.NET Core Minimal APIs, so the Azure CLI flow is part of the expected usage.

### Option 1: Fastest deployment with `az webapp up`

This is the quickest way to create the Azure resources and deploy the app from your local working directory.

Prerequisites:

- Azure CLI installed
- An Azure subscription
- .NET 10 SDK installed locally

Sign in:

```powershell
az login
```

From the repository root, run:

```powershell
az webapp up --sku F1 --name <app-name> --os-type windows --location <azure-region>
```

Replace:

- `<app-name>` with a globally unique App Service name
- `<azure-region>` with a valid Azure region such as `eastus`, `westeurope`, or `eastus2`

Notes:

- You can use `windows` or `linux` for `--os-type`. This Minimal API app is portable and can run on either.
- `F1` is the Free tier and is useful for smoke testing, but it has platform limits.
- The command creates the resource group, App Service plan, and web app, then deploys the code.

After deployment, validate the app with:

- `https://<app-name>.azurewebsites.net/`
- `https://<app-name>.azurewebsites.net/health`
- `https://<app-name>.azurewebsites.net/api/movies`

If you want the Scalar docs enabled in production for testing, set the app setting:

```powershell
az webapp config appsettings set `
  --resource-group <resource-group> `
  --name <app-name> `
  --settings ApiDocumentation__EnableInProduction=true
```

Then verify:

- `https://<app-name>.azurewebsites.net/openapi/v1.json`
- `https://<app-name>.azurewebsites.net/docs`

### Option 2: Explicit publish and ZIP deployment

Use this flow when you want a clearer build artifact and a more repeatable redeploy process.

1. Sign in and create a resource group:

```powershell
az login
az group create --name <resource-group> --location <azure-region>
```

2. Create the web app and deploy once with `az webapp up`:

```powershell
az webapp up `
  --name <app-name> `
  --resource-group <resource-group> `
  --sku B1 `
  --os-type windows `
  --location <azure-region>
```

3. Publish the app locally:

```powershell
dotnet publish .\movie-api-app-service\movie-api-app-service.csproj -c Release -o .\publish
```

4. Create a ZIP package from the contents of the publish folder:

```powershell
Compress-Archive -Path .\publish\* -DestinationPath .\movie-api-app-service.zip -Force
```

Important:

- ZIP the contents of the publish folder, not the publish folder itself.
- For ASP.NET Core on App Service, deploy the published output as the ready-to-run artifact.

5. Deploy the ZIP package:

```powershell
az webapp deploy `
  --resource-group <resource-group> `
  --name <app-name> `
  --src-path .\movie-api-app-service.zip
```

This command restarts the app after deployment.

### Useful Azure CLI Commands

Open the site in a browser:

```powershell
az webapp browse --resource-group <resource-group> --name <app-name>
```

Stream application logs:

```powershell
az webapp log tail --resource-group <resource-group> --name <app-name>
```

List valid runtime stacks for your target OS:

```powershell
az webapp list-runtimes --os-type windows
az webapp list-runtimes --os-type linux
```

### Clean Up Azure Resources

Delete the whole resource group when you are done testing:

```powershell
az group delete --name <resource-group> --yes --no-wait
```

## Project Structure

```text
movie-api-app-service.sln
movie-api-app-service/
  Api/
  Movies/
  Properties/
  Program.cs
tests/
  movie-api-app-service.Tests/
```

Key areas:

- `Program.cs`: app startup
- `Api/`: service registration, route mapping, app metadata
- `Movies/`: movie models, validation, seed loading, in-memory catalog
- `tests/`: integration coverage for API and docs behavior

## Azure App Service Use Case

This repository is designed to help test Azure App Service creation and deployment with ASP.NET Minimal APIs.

Typical reasons to use it:

- verify a new App Service can host a Minimal API successfully
- confirm routing and health checks after deployment
- confirm OpenAPI and Scalar docs are accessible
- validate configuration behavior between Development and Production
- provide a simple sample app for CI/CD or infrastructure experiments

After deployment to Azure App Service, the fastest smoke checks are:

1. Open `/health`
2. Open `/version`
3. Open `/api/movies`
4. Open `/docs` if documentation is enabled for that environment

## Notes

- The movie catalog is in-memory only.
- Created movies are lost when the app restarts.
- This app is optimized for testing and validation, not persistence-heavy production workloads.
