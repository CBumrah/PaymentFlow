using PaymentFlow.Domain.Entities;
using PaymentFlow.Domain.ValueObjects;

namespace PaymentFlow.Application.Common.Interfaces;

/// <summary>
/// Owned by the Application layer, implemented by Infrastructure — this is
/// the Dependency Inversion Principle in practice. Application depends only
/// on this abstraction, never on EF Core directly, so the persistence
/// technology could change without touching a single command handler.
///
/// Deliberately narrow and aggregate-focused (not a generic IRepository)
/// — its methods reflect real use cases the Payment aggregate needs, not a
/// blind proxy over a DbSet.
/// </summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Used to enforce idempotency — if a payment already exists for this
    /// key, the handler returns the existing payment instead of creating
    /// a duplicate.
    /// </summary>
    Task<Payment?> GetByIdempotencyKeyAsync(IdempotencyKey idempotencyKey, CancellationToken cancellationToken = default);

    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes. Exposed here (rather than a separate
    /// IUnitOfWork) for Episode 3 simplicity — a TransactionBehavior
    /// pipeline step will wrap this alongside the outbox write in Episode 5.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}