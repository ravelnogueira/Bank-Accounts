using Microsoft.AspNetCore.Mvc;

namespace Bank.Accounts.Api.Middleware;

public static class ApiProblemWriter
{
    public static Task WriteAsync(HttpContext context, int status, string title, string detail, string code,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] =
            context.Items[CorrelationIdMiddleware.ItemName]?.ToString()
            ?? context.TraceIdentifier;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(
            problem,
            cancellationToken: context.RequestAborted);
    }
}

