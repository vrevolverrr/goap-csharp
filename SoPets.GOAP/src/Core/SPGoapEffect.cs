using SoPets.GOAP.Types;

namespace SoPets.GOAP.Core;

public interface ISPGoapEffect
{
    public Datum Value { get; }
    public void ApplyEffect(SPBeliefContext current);
    public Datum TestApplyEffect(Datum? current);
}

public readonly struct EF_Set : ISPGoapEffect
{
    public required SPWorldKey Key { get; init; }
    public required Datum Value { get; init; }

    public void ApplyEffect(SPBeliefContext current)
    {
        current.SetContext(Key, Value);
    }

    public Datum TestApplyEffect(Datum? current)
    {
        return Value;
    }

    public override string ToString()
    {
        return $"Set({Value})";
    }
}

public readonly struct EF_Increment : ISPGoapEffect
{
    public required SPWorldKey Key { get; init; }
    public required Datum Value { get; init; }

    public void ApplyEffect(SPBeliefContext current)
    {
        if (current.TryGetContext(Key, out var currentValue) && currentValue != null)
        {
            current.SetContext(Key, currentValue + Value);
        }
        else
        {
            // if we increment a null value, we assume it was 0
            current.SetContext(Key, Value);
        }
    }

    public Datum TestApplyEffect(Datum? current)
    {
        if (current == null)
        {
            return Value; // if current is null, we assume it was 0
        }

        return current + Value;
    }

    public override string ToString()
    {
        return $"Inc({Value})";
    }
}

public readonly struct EF_Decrement : ISPGoapEffect
{
    public required SPWorldKey Key { get; init; }
    public required Datum Value { get; init; }

    public void ApplyEffect(SPBeliefContext current)
    {
        if (current.TryGetContext(Key, out var currentValue) && currentValue != null)
        {
            current.SetContext(Key, currentValue - Value);
        }
        else
        {
            // if we decrement a null value, we assume it was 0
            current.SetContext(Key, -Value);
        }
    }

    public Datum TestApplyEffect(Datum? current)
    {
        if (current == null)
        {
            return -Value; // if current is null, we assume it was 0
        }

        return current - Value;
    }

    public override string ToString()
    {
        return $"Dec({Value})";
    }
}

public readonly struct EF_Remove : ISPGoapEffect
{
    public required SPWorldKey Key { get; init; }

    public Datum Value => Datum.FALSE;

    public void ApplyEffect(SPBeliefContext current)
    {
        current.RemoveContext(Key);
    }

    public Datum TestApplyEffect(Datum? current)
    {
        return Value;
    }

    public override string ToString()
    {
        return $"Remove({Key})";
    }
}