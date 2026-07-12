namespace DrivingLessons.Domain.Common;

public abstract record EntityId
{
    public Guid Value { get; }

    protected EntityId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Id must not be empty.", nameof(value));
        }

        Value = value;
    }
}
