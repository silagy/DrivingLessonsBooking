using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DrivingLessons.Infrastructure.EntityFramework.EntityConfigurations.Converters;

public class SlotConstraintConverter : ValueConverter<SlotConstraint, string>
{
    public SlotConstraintConverter()
        : base(
            constraint => constraint.Value,
            value => SlotConstraint.Of(value))
    {
    }
}
