using MediatR;
using Microsoft.Extensions.Logging;
using TicketSystem.Application.Common.Interfaces;

namespace TicketSystem.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehaviour(
        ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId ?? "Anonymous";
        var userName = _currentUserService.UserName ?? "Anonymous";

        _logger.LogInformation(
            "TicketSystem Request: {Name} {@UserId} {@UserName} {@Request}",
            requestName, userId, userName, request);

        var response = await next();

        _logger.LogInformation(
            "TicketSystem Response: {Name} {@UserId} {@Response}",
            requestName, userId, response);

        return response;
    }
}
