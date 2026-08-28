using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WeatherApp.Application.Exceptions;

namespace WeatherApp.API.Exceptions;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = Map(exception);

        logger.Log(
            statusCode >= StatusCodes.Status500InternalServerError ? LogLevel.Error : LogLevel.Warning,
            exception,
            "Unhandled {ExceptionType} on {Method} {Path} -> {StatusCode}",
            exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, statusCode);

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            },
        });
    }

    /// <summary>
    /// Maps exceptions to HTTP status codes.
    /// </summary>
    /// <param name="exception">Provided exception for mapping</param>
    /// <returns></returns>
    private static (int StatusCode, string Title, string Detail) Map(Exception exception) => exception switch
    {
        WeatherApiException => (
            StatusCodes.Status502BadGateway,
            "Weather provider unavailable",
            "The upstream weather service could not be reached. Please try again shortly."),

        ArgumentException or ArgumentOutOfRangeException => (
            StatusCodes.Status400BadRequest,
            "Invalid request",
            "The request contained a value that is not valid."),

        // 499 -> nginx's "client closed request", no ASP.NET constant.
        OperationCanceledException => (
            499,
            "Request cancelled",
            "The request was cancelled before it completed."),

        // Generic on purpose -> the actual detail goes to the log
        _ => (
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred",
            "Something went wrong while handling the request."),
    };
}
