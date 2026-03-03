using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using IrlEventsWeb.Models;
using IrlEventsWeb.Services;
using IrlEventsWeb.Workers;

namespace IrlEventsWeb.Tests.Workers;

public class EventsRefreshWorkerTests
{
    private readonly Mock<IGoogleSheetsReader> _readerMock = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly NullLogger<EventsRefreshWorker> _logger = new();

    // Exposes the protected ExecuteAsync for direct testing
    private sealed class TestableWorker(
        IMemoryCache cache, IGoogleSheetsReader reader, NullLogger<EventsRefreshWorker> logger)
        : EventsRefreshWorker(cache, reader, logger)
    {
        public new Task ExecuteAsync(CancellationToken ct) => base.ExecuteAsync(ct);
    }

    private TestableWorker CreateWorker() => new(_cache, _readerMock.Object, _logger);

    [Fact]
    public async Task ExecuteAsync_OnFirstRun_FetchesEventsAndPopulatesCache()
    {
        var cts = new CancellationTokenSource();
        var events = new List<Event>
        {
            new() { Name = "Fleadh Cheoil", Category = "Music", Venue = "Dublin" }
        };
        _readerMock
            .Setup(r => r.FetchEventsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => cts.CancelAfter(50))
            .ReturnsAsync(events);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateWorker().ExecuteAsync(cts.Token));

        _readerMock.Verify(r => r.FetchEventsAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(_cache.TryGetValue(EventsRefreshWorker.CacheKey, out List<Event>? cached));
        Assert.Equal(events, cached);
    }

    [Fact]
    public async Task ExecuteAsync_AfterSuccessfulFetch_CachesCorrectEventCount()
    {
        var cts = new CancellationTokenSource();
        var events = new List<Event>
        {
            new() { Name = "Event A" },
            new() { Name = "Event B" },
            new() { Name = "Event C" },
        };
        _readerMock
            .Setup(r => r.FetchEventsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => cts.CancelAfter(50))
            .ReturnsAsync(events);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateWorker().ExecuteAsync(cts.Token));

        Assert.True(_cache.TryGetValue(EventsRefreshWorker.CacheKey, out List<Event>? cached));
        Assert.Equal(3, cached!.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReaderThrows_LogsErrorAndDoesNotCrash()
    {
        var cts = new CancellationTokenSource();
        _readerMock
            .Setup(r => r.FetchEventsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => cts.CancelAfter(50))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Exception from reader is caught inside the worker; only the cancellation surfaces
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateWorker().ExecuteAsync(cts.Token));

        _readerMock.Verify(r => r.FetchEventsAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(_cache.TryGetValue(EventsRefreshWorker.CacheKey, out _));
    }

    [Fact]
    public async Task ExecuteAsync_WhenReaderThrows_CacheRemainsUnchanged()
    {
        var cts = new CancellationTokenSource();
        var staleEvents = new List<Event> { new() { Name = "Stale Event" } };
        _cache.Set(EventsRefreshWorker.CacheKey, staleEvents);

        _readerMock
            .Setup(r => r.FetchEventsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => cts.CancelAfter(50))
            .ThrowsAsync(new Exception("Unexpected failure"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateWorker().ExecuteAsync(cts.Token));

        Assert.True(_cache.TryGetValue(EventsRefreshWorker.CacheKey, out List<Event>? cached));
        Assert.Equal(staleEvents, cached);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelledBeforeFirstIteration_DoesNotFetchEvents()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // The while-loop condition is false immediately, so ExecuteAsync returns cleanly
        await CreateWorker().ExecuteAsync(cts.Token);

        _readerMock.Verify(r => r.FetchEventsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
