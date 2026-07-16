using FluentValidation;
using MediatR;
using PaymentFlow.SharedKernel.Errors;
using PaymentFlow.SharedKernel.Primitives;

namespace PaymentFlow.Application.Common.Behaviors;

/// <summary>
/// Runs FluentValidation validators for every request before it reaches its
/// handler. If validation fails, the pipeline short-circuits and returns a
/// Result.Failure — the handler is never invoked. Constrained to
/// TResponse : Result so it only applies to requests that follow the
/// Result/Result&lt;T&gt; pattern (which is every command/query in this
/// project by convention).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        var errorMessage = string.Join(" | ", failures.Select(f => f.ErrorMessage));
        var error = Error.Validation("Validation.Failed", errorMessage);

        // Build a failed Result of the correct concrete TResponse type
        // (Result or Result<T>) via reflection, since the generic
        // constraint alone doesn't let us construct TResponse directly.
        var failureResult = BuildFailureResult(error);
        return failureResult;
    }

    private static TResponse BuildFailureResult(Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        // TResponse is Result<T> for some T — call Result.Failure<T>(error) via reflection.
        var innerType = responseType.GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, new[] { typeof(Error) })!
            .MakeGenericMethod(innerType);

        return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
    }
}