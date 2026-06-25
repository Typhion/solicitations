using Application.Common.Exceptions;
using Domain.Core;
using Microsoft.AspNetCore.Diagnostics;

namespace WebApi.Errors;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception");

        // Surface the message only for known, user-safe exception types.
        // Everything else is a 500 with no detail — never leak internal messages.
        var (status, detail) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            DomainException => (StatusCodes.Status409Conflict, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, (string?)null)
        };

        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Title = "An error occurred.",
                Status = status,
                Detail = detail
            }
        });
    }
}