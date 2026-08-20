using Microsoft.Extensions.Configuration;

namespace Infrastructure.Ai;

public sealed class EmbeddingRequestRateLimiter
{
    private readonly object _sync = new();
    private readonly Queue<DateTimeOffset> _minuteRequests = new();
    private readonly int _requestsPerMinute;
    private readonly int _requestsPerDay;
    private DateOnly _currentDay;
    private int _dayRequestCount;

    public EmbeddingRequestRateLimiter(IConfiguration configuration)
    {
        _requestsPerMinute = Math.Max(1, configuration.GetValue("Embedding:RateLimit:RequestsPerMinute", 2));
        _requestsPerDay = Math.Max(1, configuration.GetValue("Embedding:RateLimit:RequestsPerDay", 10));
        _currentDay = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
    }

    public async Task WaitForSlotAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            TimeSpan? delay;
            lock (_sync)
            {
                var now = DateTimeOffset.UtcNow;
                var today = DateOnly.FromDateTime(now.UtcDateTime);
                if (today != _currentDay)
                {
                    _currentDay = today;
                    _dayRequestCount = 0;
                    _minuteRequests.Clear();
                }

                while (_minuteRequests.TryPeek(out var timestamp) && now - timestamp >= TimeSpan.FromMinutes(1))
                {
                    _minuteRequests.Dequeue();
                }

                if (_dayRequestCount >= _requestsPerDay)
                {
                    throw new InvalidOperationException($"Embedding daily request limit reached ({_requestsPerDay} requests/day). Try again tomorrow or raise Embedding:RateLimit:RequestsPerDay.");
                }

                if (_minuteRequests.Count < _requestsPerMinute)
                {
                    _minuteRequests.Enqueue(now);
                    _dayRequestCount++;
                    return;
                }

                var oldest = _minuteRequests.Peek();
                delay = oldest.AddMinutes(1) - now;
                if (delay <= TimeSpan.Zero)
                {
                    delay = TimeSpan.FromSeconds(1);
                }
            }

            await Task.Delay(delay.Value, cancellationToken);
        }
    }
}
