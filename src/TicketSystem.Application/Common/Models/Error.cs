namespace TicketSystem.Application.Common.Models;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");

    public static Error NotFound(string entityName, object key) =>
        new($"{entityName}.NotFound", $"{entityName} with key '{key}' was not found.");

    public static Error Validation(string propertyName, string message) =>
        new($"Validation.{propertyName}", message);

    public static Error Conflict(string message) =>
        new("Error.Conflict", message);

    public static Error Unauthorized(string message = "Unauthorized access") =>
        new("Error.Unauthorized", message);

    public static Error Forbidden(string message = "Access denied") =>
        new("Error.Forbidden", message);
}
