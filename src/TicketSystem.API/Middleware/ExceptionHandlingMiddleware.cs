using System.Net;
using System.Text.Json;
using TicketSystem.Application.Common.Exceptions;

namespace TicketSystem.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Validation Error",
                Detail = "One or more validation errors occurred.",
                Errors = validationEx.Errors
            },
            NotFoundException notFoundEx => new ErrorResponse
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Not Found",
                Detail = notFoundEx.Message
            },
            ForbiddenAccessException forbiddenEx => new ErrorResponse
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Forbidden",
                Detail = forbiddenEx.Message
            },
            UnauthorizedAccessException unauthorizedEx => new ErrorResponse
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = unauthorizedEx.Message
            },
            _ => new ErrorResponse
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Server Error",
                Detail = "An unexpected error occurred"
            }
        };

        context.Response.StatusCode = response.Status;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

public class ErrorResponse
{
    public int Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
}
