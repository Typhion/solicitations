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

        context.Response.StatusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            DomainException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Title = "An error occurred.",
                Status = context.Response.StatusCode
            }
        });
    }
}