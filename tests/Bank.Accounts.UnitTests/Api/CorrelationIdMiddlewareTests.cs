using Microsoft.AspNetCore.Http;
using Bank.Accounts.Api.Middleware;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bank.Accounts.UnitTests.Api;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Invoke_WithClientCorrelationId_PropagatesValue()
    {
        const string correlationId = "test-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        context.Response.Body = new MemoryStream();
        var middleware = new CorrelationIdMiddleware(
            _ =>
            {
                Assert.Equal(
                    correlationId,
                    context.Items[CorrelationIdMiddleware.ItemName]);
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        Assert.Equal(
            correlationId,
            context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task Invoke_WithoutCorrelationId_GeneratesValue()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        Assert.True(
            Guid.TryParse(
                context.Response.Headers[CorrelationIdMiddleware.HeaderName],
                out _));
    }
}

