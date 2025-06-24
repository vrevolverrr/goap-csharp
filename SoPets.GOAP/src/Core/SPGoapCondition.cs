namespace SoPets.GOAP;

public readonly struct SPGoapCondition(
    SPWorldKey key, ISPGoapRequirement requirement, uint missingBeliefPenalty = 1)
{
    public readonly SPWorldKey key = key;
    public readonly ISPGoapRequirement requirement = requirement;
    public readonly float MissingBeliefPenalty = missingBeliefPenalty;

    public readonly bool IsSatisfied(SPWorldKey key, Datum value)
    {
        if (this.key != key) return false;
        return requirement.IsSatisfied(value);
    }

    /// <summary>
    /// Applies the effect of the given `ISPGoapEffect` to this condition's requirement.
    /// </summary>
    /// <param name="effect">The effect to apply</param>
    /// <returns>A new `SPGoapCondition` with the updated requirement</returns>
    public SPGoapCondition ApplyEffect(ISPGoapEffect effect)
    {
        return new SPGoapCondition(
            key,
            requirement.ApplyEffect(effect)
        );
    }

    /// <summary>
    /// Attempts to merge this condition with another condition, essentially
    /// performing a logical AND operation on the requirements.
    /// 
    /// If the keys do not match, or if the requirements cannot be merged due to a
    /// logical conflict, this method returns null.
    /// </summary>
    /// <param name="other"></param>
    /// <returns>A new `SPGoapCondition` containing the merged requirements, or null if the merging is not possible.</returns>
    public SPGoapCondition? MergeWith(SPGoapCondition other)
    {
        if (key != other.key) return null;
        var mergedRequirement = requirement.MergeWith(other.requirement);
        if (mergedRequirement == null) return null;
        return new SPGoapCondition(key, mergedRequirement);
    }

    /// <summary>
    /// Calculates the distance between the belief value and the requirement.
    /// If the requirement is already satisfied, the distance is 0.
    /// This is used as the heuristic for the A* search in the planner.
    /// </summary>
    /// <param name="beliefValue">The current belief value</param>
    /// <returns></returns>
    public readonly float CalculateDistance(Datum beliefValue)
    {
        if (requirement.IsSatisfied(beliefValue)) return 0;

        return beliefValue.DistanceTo(requirement.Value);
    }

    public override string ToString()
    {
        return $"{key} {requirement?.ToString() ?? "(Missing Requirement)"}";
    }
}