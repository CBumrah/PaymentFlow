using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using PaymentFlow.SharedKernel.Primitives;

namespace PaymentFlow.Application.Common.Behaviors;

/// <summary>
/// Logs the start, outcome, and duration of every request that flows
/// through MediatR, using structured logging (named properties, not
/// string concatenation) so log entries stay queryable in Serilog/Seq.
/// Registered to run before ValidationBehavior so failed validation
/// attempts are still logged, not silently dropped.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();

            stopwatch.Stop();

            if (response is Result { IsFailure: true } failedResult)
            {
                _logger.LogWarning(
                    "{RequestName} failed after {ElapsedMs}ms — {ErrorCode}: {ErrorMessage}",
                    requestName, stopwatch.ElapsedMilliseconds,
                    failedResult.Error.Code, failedResult.Error.Message);
            }
            else
            {
                _logger.LogInformation(
                    "Handled {RequestName} successfully in {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Unhandled exception in {RequestName} after {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}