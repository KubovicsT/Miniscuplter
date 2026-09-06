using System.Globalization;

namespace Miniscuplter.Core;

public interface IStrongId
{
    Guid Value { get; }
}

public readonly record struct ProjectId(Guid Value) : IStrongId
{
    public static ProjectId New() => new(Guid.NewGuid());
    public static ProjectId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString("N", CultureInfo.InvariantCulture);
}

public readonly record struct ObjectId(Guid Value) : IStrongId
{
    public static ObjectId New() => new(Guid.NewGuid());
    public static ObjectId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString("N", CultureInfo.InvariantCulture);
}

public readonly record struct RevisionId(Guid Value) : IStrongId
{
    public static RevisionId New() => new(Guid.NewGuid());
    public static RevisionId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString("N", CultureInfo.InvariantCulture);
}

public readonly record struct SelectionId(Guid Value) : IStrongId
{
    public static SelectionId New() => new(Guid.NewGuid());
    public static SelectionId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString("N", CultureInfo.InvariantCulture);
}

public readonly record struct RigId(Guid Value) : IStrongId
{
    public static RigId New() => new(Guid.NewGuid());
    public static RigId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString("N", CultureInfo.InvariantCulture);
}

public readonly record struct AttachmentId(Guid Value) : IStrongId
{
    public static AttachmentId New() => new(Guid.NewGuid());
    public static AttachmentId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString("N", CultureInfo.InvariantCulture);
}

public readonly record struct CandidateId(Guid Value) : IStrongId
{
    public static CandidateId New() => new(Guid.NewGuid());
    public static CandidateId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString("N", CultureInfo.InvariantCulture);
}

public readonly record struct TransactionId(Guid Value) : IStrongId
{
    public static TransactionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N", CultureInfo.InvariantCulture);
}
