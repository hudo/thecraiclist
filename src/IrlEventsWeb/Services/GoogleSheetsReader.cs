using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using IrlEventsWeb.Models;
using IrlEventsWeb.Workers;
using System.Net.Http.Headers;

namespace IrlEventsWeb.Services;

public interface IGoogleSheetsReader
{
    Task<List<Event>> FetchEventsAsync(CancellationToken ct = default);
    Task<List<Event>> GetCachedEventsAsync();
}

public class GoogleSheetsReader : IGoogleSheetsReader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GoogleSheetsReader> _logger;
    private readonly IWebHostEnvironment _environment;

    private readonly string _spreadsheetId;
    private readonly int _sheetGid;
    private readonly string _apiKey;
    private readonly TimeSpan _httpTimeout;

    private static readonly string FallbackFilePath = Path.Combine(AppContext.BaseDirectory, "sheets-fallback.json");
    private static readonly TimeSpan FallbackMaxAge = TimeSpan.FromDays(2);

    // Column header names from the spreadsheet mapped to Event properties
    private static readonly Dictionary<string, string> HeaderToProperty = new(StringComparer.OrdinalIgnoreCase)
    {
        ["."] = nameof(Event.Category),
        ["event"] = nameof(Event.Name),
        ["start date"] = nameof(Event.StartDate),
        ["venue"] = nameof(Event.Venue),
        ["link"] = nameof(Event.Link),
    };

    public GoogleSheetsReader(IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<GoogleSheetsReader> logger,
        IWebHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
        _environment = environment;

        _spreadsheetId = configuration["GoogleSheets:SheetId"]
            ?? throw new InvalidOperationException("GoogleSheets:SheetId configuration is missing.");
        _sheetGid = int.Parse(configuration["GoogleSheets:SheetGid"] ?? "1408471462");
        _apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
            ?? throw new InvalidOperationException("GOOGLE_API_KEY environment variable is not set.");

        _httpTimeout = _environment.IsDevelopment() ? TimeSpan.FromSeconds(3) : TimeSpan.FromSeconds(15);
    }

    public async Task<List<Event>> GetCachedEventsAsync()
    {
        if (_cache.TryGetValue(EventsRefreshWorker.CacheKey, out List<Event>? events) && events is not null)
            return events;

        return await ReadFallbackFileAsync();
    }

    public async Task<List<Event>> FetchEventsAsync(CancellationToken ct = default)
    {
        try
        {
            var events = await FetchEventsFromApiAsync(ct);

            // Persist to local file for fallback
            try
            {
                if (events.Count > 0)
                {
                    var json = JsonSerializer.Serialize(events);
                    await File.WriteAllTextAsync(FallbackFilePath, json, ct);
                    _logger.LogInformation("Saved {Count} events to fallback file.", events.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write fallback file.");
            }

            return events;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Google Sheets API call failed. Attempting fallback file.");
            return [];
        }
    }

    private async Task<List<Event>> FetchEventsFromApiAsync(CancellationToken ct)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = _httpTimeout;
        httpClient.DefaultRequestHeaders.Referrer = new Uri("http://localhost");
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TheCraicList/1.0");
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Resolve sheet name from GID via spreadsheet metadata
        var sheetName = await ResolveSheetNameAsync(httpClient, ct);

        // Fetch all values from the sheet
        _logger.LogInformation("Fetching values from sheet '{SheetName}' (GID: {Gid})...", sheetName, _sheetGid);

        var valuesUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}/values/{Uri.EscapeDataString(sheetName)}?key={_apiKey}";
        var response = await httpClient.GetAsync(valuesUrl, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var valuesElement = doc.RootElement.GetProperty("values");
        var rows = new List<List<string>>();
        foreach (var row in valuesElement.EnumerateArray())
        {
            var cells = new List<string>();
            foreach (var cell in row.EnumerateArray())
            {
                cells.Add(cell.GetString() ?? "");
            }
            rows.Add(cells);
        }

        if (rows.Count == 0)
        {
            _logger.LogWarning("Google Sheets API returned no rows.");
            return [];
        }

        _logger.LogInformation("Fetched {RowCount} rows from Google Sheets API.", rows.Count);

        // First row is the header
        var headers = rows[0];
        var columnMap = BuildColumnMap(headers);

        var events = new List<Event>();
        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var evt = new Event
            {
                Category = GetCell(row, columnMap, nameof(Event.Category)),
                Name = GetCell(row, columnMap, nameof(Event.Name)),
                StartDate = DateTime.TryParseExact(GetCell(row, columnMap, nameof(Event.StartDate)), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate) ? startDate : default,
                Venue = GetCell(row, columnMap, nameof(Event.Venue)),
                Link = GetCell(row, columnMap, nameof(Event.Link)),
            };
            events.Add(evt);
        }

        _logger.LogInformation("Parsed {Count} events from Google Sheets API.", events.Count);
        return events;
    }

    private async Task<List<Event>> ReadFallbackFileAsync()
    {
        if (!File.Exists(FallbackFilePath))
        {
            _logger.LogWarning("No fallback file found at {Path}.", FallbackFilePath);
            return [];
        }

        var fileAge = DateTime.UtcNow - File.GetLastWriteTimeUtc(FallbackFilePath);
        if (fileAge > FallbackMaxAge)
        {
            _logger.LogWarning("Fallback file is {Age} old (max {Max}). Ignoring stale data.", fileAge, FallbackMaxAge);
            return [];
        }

        var json = await File.ReadAllTextAsync(FallbackFilePath);
        var events = JsonSerializer.Deserialize<List<Event>>(json) ?? [];
        _logger.LogInformation("Loaded {Count} events from fallback file ({Age} old).", events.Count, fileAge);
        return events;
    }

    private async Task<string> ResolveSheetNameAsync(HttpClient httpClient, CancellationToken ct)
    {
        _logger.LogInformation("Resolving sheet name for GID {Gid}...", _sheetGid);
        
        var metadataUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}?key={_apiKey}&fields=sheets.properties";
        
        var response = await httpClient.GetAsync(metadataUrl, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Fetched spreadsheet metadata to resolve sheet name. Content size: {ContentLength} bytes.", response.Content.Headers.ContentLength ?? 0);

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        foreach (var sheet in doc.RootElement.GetProperty("sheets").EnumerateArray())
        {
            var props = sheet.GetProperty("properties");
            if (props.GetProperty("sheetId").GetInt32() == _sheetGid)
            {
                return props.GetProperty("title").GetString()
                    ?? throw new InvalidOperationException($"Sheet with GID {_sheetGid} has no title.");
            }
        }

        throw new InvalidOperationException($"No sheet found with GID {_sheetGid} in spreadsheet {_spreadsheetId}.");
    }

    private static Dictionary<string, int> BuildColumnMap(List<string> headers)
    {
        var map = new Dictionary<string, int>();
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim();
            if (HeaderToProperty.TryGetValue(header, out var propertyName))
            {
                map[propertyName] = i;
            }
        }
        return map;
    }

    private static string GetCell(List<string> row, Dictionary<string, int> columnMap, string propertyName) =>
        columnMap.TryGetValue(propertyName, out var index) && index < row.Count
            ? row[index]
            : "";
}
