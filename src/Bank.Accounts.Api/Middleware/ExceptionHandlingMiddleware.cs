using Bank.Accounts.Domain.Common;
using Bank.Accounts.Application.Common.Errors;

namespace Bank.Accounts.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (RequestValidationException exception)
        {
            await ApiProblemWriter.WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Validation Error",
                exception.Message,
                exception.Code,
                exception.Errors);
        }
        catch (BadHttpRequestException exception)
        {
            await ApiProblemWriter.WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "The request payload or parameters are invalid.",
                ErrorCodes.ValidationError);
            logger.LogDebug(exception, "Request binding failed.");
        }
        catch (DomainRuleException exception)
        {
            await ApiProblemWriter.WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Validation Error",
                exception.Message,
                ErrorCodes.ValidationError);
        }
        catch (AccountNotFoundException exception)
        {
            await ApiProblemWriter.WriteAsync(
                context,
                StatusCodes.Status404NotFound,
                "Not Found",
                exception.Message,
                exception.Code);
        }
        catch (TaxIdAlreadyExistsException exception)
        {
            await ApiProblemWriter.WriteAsync(
                context,
                StatusCodes.Status409Conflict,
                "Conflict",
                exception.Message,
                exception.Code);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request was cancelled by the client.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request exception.");
            await ApiProblemWriter.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred.",
                ErrorCodes.InternalServerError);
        }
    }
}
