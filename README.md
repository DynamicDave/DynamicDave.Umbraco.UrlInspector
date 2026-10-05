# DynamicDave.Umbraco.UrlInspector

URLs & Redirects tab on Umbraco documents: current URLs per culture, incoming and outgoing redirects, canonical, and a guarded URL test.

Umbraco 17 backoffice package (`DynamicDave.Umbraco.UrlInspector`). See [the package README](src/DynamicDave.Umbraco.UrlInspector/README.md) for what it does, configuration and limitations.

## Prerequisites

- .NET 10 SDK
- Node.js 22+ and npm on PATH. `dotnet build` and `dotnet pack` run the Vite client build automatically (`npm ci` only when `src/DynamicDave.Umbraco.UrlInspector/Client/node_modules` is missing, then `npm run build`; repeat builds are incremental). Skip it with `-p:SkipClientBuild=true` when the client output already exists.

## Develop

    dotnet build DynamicDave.Umbraco.UrlInspector.slnx
    dotnet test DynamicDave.Umbraco.UrlInspector.slnx
    dotnet run --project tests/TestSite

The `Umbraco.Web.UI` launch profile in `tests/TestSite/Properties/launchSettings.json` sets `ASPNETCORE_ENVIRONMENT=Development` and the ports, so the command above works in any shell. The first time you may need to trust the dev certificate (`dotnet dev-certs https --trust`).

Backoffice: https://localhost:44414/umbraco (use the https URL to log in). The test admin is configured in `tests/TestSite/appsettings.Development.json` (local development only). The TestSite creates its own SQLite database on first start; delete `tests/TestSite/umbraco/Data/Umbraco.sqlite.db*` to start over.

## Pack

    dotnet pack src/DynamicDave.Umbraco.UrlInspector -c Release -o artifacts

## License

MIT, see [LICENSE](LICENSE).
