namespace AdsApi.Middleware;

public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _log;
    private const string HeaderName = "Idempotency-Key";

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> log)
    {
        _next = next; _log = log;
    }

    public async Task Invoke(HttpContext ctx)
    {
        string? key = null;
        if (ctx.Request.Headers.TryGetValue(HeaderName, out var headerVal))
        {
            key = headerVal.ToString().Trim();
            if (string.IsNullOrEmpty(key)) key = null;
        }

        if (key is not null)
        {
            RequestIdemAccessor.Current = key;
            _log.LogDebug("Idempotency key captured: {Key}", key);
        }

        try { await _next(ctx); }
        finally { RequestIdemAccessor.Current = null; }
    }
}

public static class IdempotencyMiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotencyKey(this IApplicationBuilder app)
        => app.UseMiddleware<IdempotencyMiddleware>();
}
