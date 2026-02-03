using Polly;
using Polly.Extensions.Http;
using Polly.Registry;
using Polly.Timeout;

namespace AdsApi.Infrastructure.Http;

public static class ResiliencePolicies
{
    public const string StandardHttpRetryKey = "StandardHttpRetry";
    public const string StandardHttpCircuitBreakerKey = "StandardHttpCircuitBreaker";
    public const string StandardHttpTimeoutKey = "StandardHttpTimeout";

    public static IPolicyRegistry<string> AddAdsHttpPolicies(this IServiceCollection services)
    {
        // Fix: Use PolicyRegistry directly and register it as a singleton if AddPolicyRegistry is unavailable
        var registry = new PolicyRegistry();

        // 1. Timeout: kill slow calls (e.g., 5 seconds)
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(5));

        // 2. Retry: handle transient HTTP errors + 408 + 5xx
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)) // 200, 400, 800 ms
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)) // jitter
            );

        // 3. CircuitBreaker: after N failures, open for a while
        var breakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 8,
                durationOfBreak: TimeSpan.FromSeconds(30)
            );

        var bulkheadPolicy = Policy
            .BulkheadAsync<HttpResponseMessage>(
                maxParallelization: 20,
                maxQueuingActions: 50);

        var rateLimitPolicy = Policy
            .RateLimitAsync(
                numberOfExecutions: 100,
                perTimeSpan: TimeSpan.FromMinutes(1));

        registry.Add(StandardHttpTimeoutKey, timeoutPolicy);
        registry.Add(StandardHttpRetryKey, retryPolicy);
        registry.Add(StandardHttpCircuitBreakerKey, breakerPolicy);

        // Register the policy registry in the DI container
        services.AddSingleton<IPolicyRegistry<string>>(registry);

        return registry;
    }
}
