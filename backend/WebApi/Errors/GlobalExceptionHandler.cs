using Application.Common.Exceptions;
using Domain.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Localization;

namespace WebApi.Errors;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    IStringLocalizer<Messages> localizer,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, detail) = exception switch
        {
            // Messages are shown to the user, so translate them to the request's language.
            NotFoundException nf => (StatusCodes.Status404NotFound,
                localizer["\"{0}\" ({1}) was not found.", localizer[nf.Name].Value, nf.Key].Value),
            DomainException => (StatusCodes.Status409Conflict, localizer[exception.Message].Value),
            _ => (StatusCodes.Status500InternalServerError, (string?)null)
        };
        
        if (status >= 500)
            logger.LogError(exception, "Unhandled exception");
        else
            logger.LogWarning("Request failed ({Status}): {Message}", status, exception.Message);

        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails =
            {
                Title = localizer["An error occurred."],
                Status = status,
                Detail = detail
            }
        });
    }
}