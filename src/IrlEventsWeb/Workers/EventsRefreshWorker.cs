using Microsoft.Extensions.Caching.Memory;
using IrlEventsWeb.Models;
using IrlEventsWeb.Services;

namespace IrlEventsWeb.Workers;

public class EventsRefreshWorker : BackgroundService
{
    public const string CacheKey = "events";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(3);
    private static readonly TimeSpan RefreshBeforeExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(10);

    private readonly IMemoryCache _cache;
    private readonly IGoogleSheetsReader _reader;
    private readonly ILogger<EventsRefreshWorker> _logger;
    private readonly IHostEnvironment _env;

    private DateTimeOffset _cacheExpiresAt = DateTimeOffset.MinValue;

    public EventsRefreshWorker(
        IMemoryCache cache,
        IGoogleSheetsReader reader,
        ILogger<EventsRefreshWorker> logger,
        IHostEnvironment env)
    {
        _cache = cache;
        _reader = reader;
        _logger = logger;
        _env = env;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (ShouldRefresh())
            {
                await RefreshCacheAsync(stoppingToken);
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private bool ShouldRefresh() =>
        _cacheExpiresAt == DateTimeOffset.MinValue ||
        DateTimeOffset.UtcNow >= _cacheExpiresAt - RefreshBeforeExpiration;

    private async Task RefreshCacheAsync(CancellationToken ct)
    {
        try
        {
            var timeout = _env.IsDevelopment() ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(1);
            var token = new CancellationTokenSource(timeout).Token;
            
            _logger.LogInformation("Fetching events from Google Sheets...");

            var events = await _reader.FetchEventsAsync(token);

            if (events.Count > 0)
            {
                _cache.Set(CacheKey, events, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheExpiration,
                });

                _cacheExpiresAt = DateTimeOffset.UtcNow + CacheExpiration;
                _logger.LogInformation("Cached {Count} events. Cache expires at {ExpiresAt:u}.", events.Count, _cacheExpiresAt);
            }
            else
            {
                _logger.LogWarning("No events fetched from Google Sheets. Cache not updated.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh events cache.");
        }
    }
}
