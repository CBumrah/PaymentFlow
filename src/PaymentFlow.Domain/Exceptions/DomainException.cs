namespace PaymentFlow.Domain.Exceptions;

/// <summary>
/// Base type for all exceptions representing a violation of a domain
/// invariant — as opposed to infrastructure failures (network, database)
/// which should surface as their own distinct exception types.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}