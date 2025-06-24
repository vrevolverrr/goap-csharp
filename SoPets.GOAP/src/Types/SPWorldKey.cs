namespace SoPets.GOAP;

public enum SPBeliefKey
{
    HUNGER,
    HAPPINESS,
    ENERGY,

    IS_AT_POSITION,
    TARGET_POSITION,

    SAW_ITEM,

    TARGET_FOOD,

    IS_WANDERING,
}

public enum SPInstanceType
{
    NONE,
    FOOD,
    FURNITURE,
    PET,
    TOY,
}

public readonly partial struct SPWorldKey(SPBeliefKey beliefKey, SPInstanceType instanceType, uint? instanceId)
{
    public readonly SPBeliefKey BeliefKey { get; init; } = beliefKey;

    public readonly SPInstanceType InstanceType { get; init; } = instanceType;
    public readonly uint? InstanceId { get; init; } = instanceId;

    public SPWorldKey(SPBeliefKey beliefKey)
        : this(beliefKey, SPInstanceType.NONE, null) { }

    public bool Equals(SPWorldKey other)
    {
        return BeliefKey == other.BeliefKey &&
               InstanceType == other.InstanceType &&
               InstanceId == other.InstanceId;
    }

    public override bool Equals(object? obj)
    {
        return obj is SPWorldKey key && Equals(key);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BeliefKey, InstanceType, InstanceId);
    }

    public static bool operator ==(SPWorldKey left, SPWorldKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SPWorldKey left, SPWorldKey right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        if (InstanceType != SPInstanceType.NONE && InstanceId.HasValue)
        {
            return $"{BeliefKey} ({InstanceType},{InstanceId.Value})";
        }

        return $"{BeliefKey}";
    }
}