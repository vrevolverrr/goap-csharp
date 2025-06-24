
namespace SoPets.GOAP;

// TODO: write tests for these comparers, fucking hope its correct
public interface ISPGoapRequirement
{
    public Datum Value { get; }
    public bool IsSatisfied(Datum other);
    public ISPGoapRequirement ApplyEffect(ISPGoapEffect effect);
    public ISPGoapRequirement? MergeWith(ISPGoapRequirement other);
}

public readonly struct GR_GreaterEqual(Datum value) : ISPGoapRequirement
{
    private readonly Datum value = value;
    public Datum Value => value;

    public ISPGoapRequirement ApplyEffect(ISPGoapEffect effect)
    {
        return effect switch
        {
            EF_Increment incEffect => new GR_GreaterEqual(value - incEffect.Value),
            EF_Decrement decEffect => new GR_GreaterEqual(value + decEffect.Value),
            _ => throw new ArgumentException($"Unsupported effect type: {effect.GetType()}")
        };
    }

    public readonly bool IsSatisfied(Datum other)
    {
        return other >= value;
    }

    public ISPGoapRequirement? MergeWith(ISPGoapRequirement other)
    {
        switch (other)
        {
            case GR_GreaterEqual otherGE:
                return new GR_GreaterEqual(Datum.Max(value, otherGE.value));

            case GR_InBetween otherIB:
                if (value > otherIB.max)
                    return null;
                if (value == otherIB.max)
                    return new GR_Equal(value);
                if (value >= otherIB.min) // value <= otherIB.max is implicitly true here
                    return new GR_InBetween(value, otherIB.max);

                // here, value < otherIB.min
                return otherIB;

            case GR_LessEqual otherLE:
                if (otherLE.Value < value)
                    return null;
                if (otherLE.Value == value)
                    return new GR_Equal(value);

                return new GR_InBetween(value, otherLE.Value);

            case GR_Equal otherEq:
                return otherEq.Value >= value ? other : null;

            default:
                return null;
        }
    }

    public override string ToString()
    {
        return $">= {value}";
    }
}

public readonly struct GR_LessEqual(Datum value) : ISPGoapRequirement
{
    private readonly Datum value = value;
    public Datum Value => value;

    public ISPGoapRequirement ApplyEffect(ISPGoapEffect effect)
    {
        return effect switch
        {
            EF_Increment incEffect => new GR_LessEqual(value - incEffect.Value),
            EF_Decrement decEffect => new GR_LessEqual(value + decEffect.Value),
            _ => throw new ArgumentException($"Unsupported effect type: {effect.GetType()}")
        };
    }

    public readonly bool IsSatisfied(Datum other)
    {
        return other <= value;
    }

    public ISPGoapRequirement? MergeWith(ISPGoapRequirement other)
    {
        switch (other)
        {
            case GR_LessEqual otherLE:
                return new GR_LessEqual(Datum.Min(value, otherLE.value));

            case GR_InBetween otherIB:
                if (value < otherIB.min)
                    return null;
                if (value == otherIB.min)
                    return new GR_Equal(value);
                if (value <= otherIB.max) // value >= otherIB.min is implicitly true here
                    return new GR_InBetween(otherIB.min, value);

                // here, value > otherIB.max
                return otherIB;

            case GR_GreaterEqual otherGE:
                if (otherGE.Value > value)
                    return null;
                if (otherGE.Value == value)
                    return new GR_Equal(value);

                return new GR_InBetween(otherGE.Value, value);

            case GR_Equal otherEq:
                return otherEq.Value <= value ? other : null;

            default:
                return null;
        }
    }

    public override string ToString()
    {
        return $"<= {value}";
    }
}

public readonly struct GR_Equal(Datum value) : ISPGoapRequirement
{
    private readonly Datum value = value;
    public Datum Value => value;

    public ISPGoapRequirement ApplyEffect(ISPGoapEffect effect)
    {
        return effect switch
        {
            EF_Increment incEffect => new GR_Equal(value - incEffect.Value),
            EF_Decrement decEffect => new GR_Equal(value + decEffect.Value),
            _ => throw new ArgumentException($"Invalid effect type {effect.GetType()} for comparison"),
        };
    }

    public readonly bool IsSatisfied(Datum other)
    {
        return other == value;
    }

    public ISPGoapRequirement? MergeWith(ISPGoapRequirement other)
    {
        return other switch
        {
            GR_Equal otherEq => value == otherEq.value ? this : null,
            GR_GreaterEqual otherGE => value >= otherGE.Value ? this : null,
            GR_LessEqual otherLE => value <= otherLE.Value ? this : null,
            GR_InBetween otherIB => value >= otherIB.min && value <= otherIB.max ? this : null,
            _ => null
        };
    }

    public override string ToString()
    {
        return $"== {value}";
    }
}

public readonly struct GR_InBetween : ISPGoapRequirement
{
    public readonly Datum min;
    public readonly Datum max;
    public Datum Value => Datum.Midpoint(min, max);

    public GR_InBetween(Datum min, Datum max)
    {
        if (min > max)
            throw new ArgumentException("Min cannot be greater than Max in GR_InBetween");

        if (min == max)
            throw new ArgumentException("Min and Max cannot be equal in GR_InBetween, use GR_Equal instead");

        this.min = min;
        this.max = max;
    }

    public ISPGoapRequirement ApplyEffect(ISPGoapEffect effect)
    {
        return effect switch
        {
            EF_Increment incEffect => new GR_InBetween(min - incEffect.Value, max - incEffect.Value),
            EF_Decrement decEffect => new GR_InBetween(min + decEffect.Value, max + decEffect.Value),
            _ => throw new ArgumentException($"Invalid effect type {effect.GetType()} for comparison"),
        };
    }

    public readonly bool IsSatisfied(Datum other)
    {
        return other >= min && other <= max;
    }

    public ISPGoapRequirement? MergeWith(ISPGoapRequirement other)
    {
        switch (other)
        {
            case GR_InBetween otherIB:
                var newMin = Datum.Max(this.min, otherIB.min);
                var newMax = Datum.Min(this.max, otherIB.max);

                // Check if the resulting range is valid
                if (newMin > newMax) return null; // Conflict
                if (newMin == min && newMax == max) return this; // No change
                if (newMin == otherIB.min && newMax == otherIB.max) return other; // No change
                if (newMin == newMax) return new GR_Equal(newMin); // Single point

                return new GR_InBetween(newMin, newMax);

            case GR_GreaterEqual otherGE:
                if (otherGE.Value > max)
                    return null;
                if (otherGE.Value == max)
                    return new GR_Equal(otherGE.Value);
                if (otherGE.Value >= min) // value <= otherIB.max is implicitly true here
                    return new GR_InBetween(otherGE.Value, max);

                // here, otherGE.value < min
                return this;

            case GR_LessEqual otherLE:
                if (otherLE.Value < min)
                    return null;
                if (otherLE.Value == min)
                    return new GR_Equal(otherLE.Value);
                if (otherLE.Value <= max) // otherLE.value >= min is implicitly true here
                    return new GR_InBetween(min, otherLE.Value);

                // here, otherLE.value > max
                return this;

            case GR_Equal otherEq:
                return otherEq.Value <= max && otherEq.Value >= min ? otherEq : null;

            default:
                return null;
        }
    }

    public override string ToString()
    {
        return $"∈ [{min}, {max}]";
    }
}
// TODO refactor exisiting ISPGoapRequirement implementations to use the new class