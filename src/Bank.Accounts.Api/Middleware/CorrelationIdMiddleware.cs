namespace Bank.Accounts.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemName = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = !string.IsNullOrWhiteSpace(supplied) &&
                            supplied.Length <= 100
            ? supplied
            : Guid.NewGuid().ToString();

        context.Items[ItemName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(
                   new Dictionary<string, object>
                   {
                       [ItemName] = correlationId
                   }))
        {
            await next(context);
        }
    }
}
