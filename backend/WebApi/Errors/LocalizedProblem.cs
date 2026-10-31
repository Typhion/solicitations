using Microsoft.Extensions.Localization;

namespace WebApi.Errors;

/// <summary>
/// A ProblemDetails response whose messages are translated to the request's language
/// (<see cref="Messages"/>) before they're joined into <c>detail</c>.
/// </summary>
public static class LocalizedProblem
{
    public static IResult For(IEnumerable<string> messages, int statusCode) => new Result(messages.ToList(), statusCode);

    public static IResult For(string? message, int statusCode) => For(message is null ? [] : [message], statusCode);

    private sealed class Result(IReadOnlyList<string> messages, int statusCode) : IResult
    {
        public Task ExecuteAsync(HttpContext context)
        {
            var localizer = context.RequestServices.GetRequiredService<IStringLocalizer<Messages>>();
            var detail = messages.Count == 0 ? null : string.Join(" ", messages.Select(m => localizer[m].Value));
            return Results.Problem(detail: detail, statusCode: statusCode).ExecuteAsync(context);
        }
    }
}
