using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Caching.Memory;
using IrlEventsWeb.Models;
using IrlEventsWeb.Workers;

namespace IrlEventsWeb.Services;

public interface IGoogleSheetsReader
{
    Task<List<Event>> FetchEventsAsync(CancellationToken ct = default);
    List<Event> GetCachedEvents();
}

public class GoogleSheetsReader : IGoogleSheetsReader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GoogleSheetsReader> _logger;

    private readonly string _csvUrl;

    public GoogleSheetsReader(IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<GoogleSheetsReader> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;

        var sheetId = configuration["GoogleSheets:SheetId"]
            ?? throw new InvalidOperationException("GoogleSheets:SheetId configuration is missing.");
        _csvUrl = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv";
    }

    public List<Event> GetCachedEvents() =>
        _cache.TryGetValue(EventsRefreshWorker.CacheKey, out List<Event>? events) && events is not null
            ? events
            : [];

    public async Task<List<Event>> FetchEventsAsync(CancellationToken ct = default)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var csvContent = await httpClient.GetStringAsync(_csvUrl, ct);

        _logger.LogInformation("Fetched CSV content from Google Sheets (length: {Length} characters).", csvContent.Length);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<EventClassMap>();

        var events = csv.GetRecords<Event>().ToList();
        _logger.LogInformation("Deserialised {Count} events from Google Sheets.", events.Count);

        return events;
    }
}
