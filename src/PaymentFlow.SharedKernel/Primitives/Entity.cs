namespace PaymentFlow.SharedKernel.Primitives;

/// <summary>
/// Base class for all domain entities. Equality is based on identity (Id),
/// not property values - two entities with the same Id are considered the
/// same entity even if other properties differ.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected init; }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entity Id cannot be empty.", nameof(id));
        }

        Id = id;
    }

    // Required by EF Core for materialization.
    protected Entity()
    {
    }

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => (GetType(), Id).GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}