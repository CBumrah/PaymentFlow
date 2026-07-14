namespace PaymentFlow.SharedKernel.Errors;

/// <summary>
/// Represents an expected business/domain failure (e.g., "payment already
/// captured"). Paired with Result&lt;T&gt; so handlers can return failures
/// without throwing exceptions for conditions that are part of normal
/// business flow — exceptions are reserved for truly exceptional cases.
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string code, string message) => new(code, message);

    public static Error Validation(string code, string message) => new(code, message);

    public static Error Conflict(string code, string message) => new(code, message);

    public static Error Failure(string code, string message) => new(code, message);
}