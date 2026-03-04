# Copilot Instructions for TheCraicList

## Build, test, and run commands
- Build solution: `dotnet build src/src.slnx`
- Run web app: `dotnet run --project src/IrlEventsWeb/IrlEventsWeb.csproj`
- Run all tests: `dotnet test src/src.slnx`
- Run a single test: `dotnet test src/IrlEventsWeb.Tests/IrlEventsWeb.Tests.csproj --filter "FullyQualifiedName~ExecuteAsync_OnFirstRun_FetchesEventsAndPopulatesCache"`
- No dedicated lint/format command is configured in this repository.

## High-level architecture
- The app is an ASP.NET Core MVC site (`src/IrlEventsWeb`) with a single hosted background worker, plus xUnit tests (`src/IrlEventsWeb.Tests`), all in `src/src.slnx`.
- `Program.cs` wires `IGoogleSheetsReader` as a singleton service and starts `EventsRefreshWorker` as a hosted service; controllers and Razor views read from in-memory cache rather than fetching per-request.
- `GoogleSheetsReader` resolves a worksheet name by GID through Google Sheets API metadata, then fetches values, maps spreadsheet headers to `Event` fields, and parses `StartDate` with `dd/MM/yyyy`.
- `EventsRefreshWorker` owns refresh cadence and cache lifetime (`PollingInterval=10m`, refresh 30m before a 3h expiration) and writes events into `IMemoryCache` under `EventsRefreshWorker.CacheKey`.
- `HomeController` groups cached events by category for the index page, limits category previews to 10 events, limits per-category page results to 150, and exposes `/category/{name}` via a named route.

## Key repository conventions
- Runtime configuration is split between appsettings and environment variables: `GoogleSheets:SheetId` and `GoogleSheets:SheetGid` come from config, while `GOOGLE_API_KEY` must be present as an environment variable.
- Spreadsheet parsing is header-driven via `HeaderToProperty` in `GoogleSheetsReader`; expected source headers are `"."`, `"event"`, `"start date"`, `"venue"`, and `"link"`.
- When cached data is missing, services/controllers return empty collections and the UI derives all category structure from available events at runtime.
- Category styling is centralized in `HomeController.CategoryStyles`; unknown categories must fall back to the default style.
- Test pattern for `BackgroundService` is to expose `ExecuteAsync` via a test-only subclass and force loop exit with cancellation (`CancelAfter`) while asserting cache effects.
- Ignore non-production playground code (such as `pocs/`) unless explicitly asked to work on it.
