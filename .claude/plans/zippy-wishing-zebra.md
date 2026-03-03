# Plan: Google Sheets Background Cache Worker

## Context

The production app (`src/`) is a minimal ASP.NET Core 10.0 starter with a single "Hello World" endpoint and no dependencies. The goal is to add a background worker that periodically fetches Irish cultural events from a Google Sheets CSV export, parses them into a typed model, and stores them in an in-memory cache. The cache expires after 3 hours; the worker runs every 10 minutes and proactively refreshes data 30 minutes before expiration (i.e., at the 2.5-hour mark). A reusable `GoogleSheetsReader` service handles all fetching and parsing logic.

The POC (`pocs/console-reader/`) already demonstrates the fetch-and-parse approach using CsvHelper v33.1.0 with `HttpClient` and `System.Text.Json`. The same patterns will be reused in production.

---

## Implementation Plan

### 1. Add NuGet dependency

**File:** `src/IrlEventsWeb.csproj`

Add CsvHelper (same version as POC):
```xml
<PackageReference Include="CsvHelper" Version="33.1.0" />
```

---

### 2. Add configuration

**File:** `src/appsettings.json`

Add a `GoogleSheets` section:
```json
"GoogleSheets": {
  "SheetId": "1koOd6LfRzT54TmawJcuzJH0rbG218-1wsojIlx1zDtE"
}
```

---

### 3. Create `Event` model

**File:** `src/Models/Event.cs`

```csharp
namespace IrlEventsWeb.Models;

public class Event
{
    public string Category { get; set; } = "";
    public string Name { get; set; } = "";
    public string StartDate { get; set; } = "";
    public string DateAdded { get; set; } = "";
    public string Venue { get; set; } = "";
    public string Link { get; set; } = "";
}
```

---

### 4. Create `GoogleSheetsReader` service

**File:** `src/Services/GoogleSheetsReader.cs`

- Constructor takes `IHttpClientFactory` and `IConfiguration`
- Builds URL: `https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv`
- Uses `CsvReader` with `CultureInfo.InvariantCulture` and a `ClassMap` to handle the `"."` → `Category` column rename
- Returns `List<Event>`

**ClassMap for column rename:**
```csharp
public sealed class EventClassMap : ClassMap<Event>
{
    public EventClassMap()
    {
        Map(m => m.Category).Name(".");
        Map(m => m.Name).Name("event");
        Map(m => m.StartDate).Name("start date");
        Map(m => m.DateAdded).Name("new");
        Map(m => m.Venue).Name("venue");
        Map(m => m.Link).Name("link");
    }
}
```

**Public method:**
```csharp
public async Task<List<Event>> FetchEventsAsync(CancellationToken ct = default)
```

Register as singleton in DI (`IHttpClientFactory` is thread-safe).

---

### 5. Create `EventsRefreshWorker` background service

**File:** `src/Workers/EventsRefreshWorker.cs`

- Inherits `BackgroundService`
- Injects `IMemoryCache`, `GoogleSheetsReader`, `ILogger<EventsRefreshWorker>`
- Tracks cache expiration with a private field: `DateTimeOffset _cacheExpiresAt = DateTimeOffset.MinValue`
- Cache key constant: `"events"`

**`ExecuteAsync` loop (runs every 10 minutes):**
```
loop:
  if ShouldRefresh() → await RefreshCacheAsync()
  await Task.Delay(10 minutes, stoppingToken)
```

**`ShouldRefresh()`:**
```csharp
// Refresh if cache is unset OR expires within 30 minutes
return DateTimeOffset.UtcNow >= _cacheExpiresAt - TimeSpan.FromMinutes(30);
```

**`RefreshCacheAsync()`:**
1. Call `_reader.FetchEventsAsync(ct)`
2. Set cache entry with `AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3)`
3. Update `_cacheExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromHours(3)`
4. Log success or catch/log exceptions (don't crash the worker on errors)

---

### 6. Update `Program.cs`

**File:** `src/Program.cs`

Register services before `builder.Build()`:
```csharp
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();                        // IHttpClientFactory
builder.Services.AddSingleton<GoogleSheetsReader>();
builder.Services.AddHostedService<EventsRefreshWorker>();
```

---

## Files to Create/Modify

| File | Action |
|------|--------|
| `src/IrlEventsWeb.csproj` | Add CsvHelper 33.1.0 package reference |
| `src/appsettings.json` | Add `GoogleSheets.SheetId` config key |
| `src/Models/Event.cs` | **Create** — typed event model |
| `src/Services/GoogleSheetsReader.cs` | **Create** — fetch + parse service |
| `src/Workers/EventsRefreshWorker.cs` | **Create** — background cache refresh worker |
| `src/Program.cs` | Register services (AddMemoryCache, AddHttpClient, AddSingleton, AddHostedService) |

---

## Verification

1. `dotnet build src/` — should build with no errors after CsvHelper is restored
2. `dotnet run --project src/` — app starts; within seconds the worker logs "Fetched N events" (first run hits `ShouldRefresh` immediately since `_cacheExpiresAt` is `MinValue`)
3. Add a temporary `GET /events` endpoint returning `IMemoryCache` contents to confirm events are cached and properly mapped (category, name, startDate, etc.)
4. Confirm "." column maps to `Category` and not `"."` in the JSON output
