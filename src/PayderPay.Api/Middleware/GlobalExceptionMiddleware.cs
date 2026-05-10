using System.Net;
using System.Text.Json;
using PayderPay.Application.Exceptions;

namespace PayderPay.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = MapException(exception);

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception.");
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {StatusCode}: {Message}", (int)statusCode, exception.Message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var payload = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail = exception.Message,
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(payload);
        await context.Response.WriteAsync(json);
    }

    private static (HttpStatusCode StatusCode, string Title) MapException(Exception exception)
    {
        return exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Resource Not Found"),
            ConflictException => (HttpStatusCode.Conflict, "Conflict"),
            BadRequestException => (HttpStatusCode.BadRequest, "Bad Request"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Bad Request"),
            ArgumentException => (HttpStatusCode.BadRequest, "Bad Request"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };
    }
}
