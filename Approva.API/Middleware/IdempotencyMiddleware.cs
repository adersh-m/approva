using Approva.API.Application.Interfaces;

namespace Approva.API.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IIdempotencyService idempotencyService)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey)
            || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                "{\"success\":false,\"data\":null,\"error\":\"Idempotency-Key header is required\"}");
            return;
        }

        var redisKey = $"idempotency:{context.Request.Path}:{idempotencyKey}";

        var cached = await idempotencyService.GetResultAsync(redisKey);
        if (cached is not null)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cached);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            buffer.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(buffer).ReadToEndAsync();

            if (context.Response.StatusCode is >= 200 and < 300)
                await idempotencyService.StoreResultAsync(redisKey, responseBody, TimeSpan.FromHours(24));

            buffer.Seek(0, SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
