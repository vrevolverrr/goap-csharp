using SoPets.GOAP.Types;

namespace SoPets.GOAP.Core;

public class SPBeliefContext : SPGoapContext<SPWorldKey, Datum>
{
    private readonly Dictionary<SPBeliefKey, Dictionary<SPWorldKey, Datum>> _beliefGroups;

    public SPBeliefContext() : base()
    {
        _beliefGroups = [];
    }

    public SPBeliefContext(SPWorldKey key, Datum value) : base(key, value)
    {
        _beliefGroups = [];
        SetContext(key, value);
    }

    public SPBeliefContext(Dictionary<SPWorldKey, Datum> contexts) : base(contexts)
    {
        _beliefGroups = [];

        foreach (var kvp in contexts)
        {
            if (!_beliefGroups.TryGetValue(kvp.Key.BeliefKey, out var group))
            {
                group = [];
                _beliefGroups[kvp.Key.BeliefKey] = group;
            }

            group[kvp.Key] = kvp.Value;
        }
    }

    public new void SetContext(SPWorldKey key, Datum value)
    {
        // Store in the main context
        base.SetContext(key, value);

        // Also store in the belief group
        if (!_beliefGroups.TryGetValue(key.BeliefKey, out var group))
        {
            group = [];
            _beliefGroups[key.BeliefKey] = group;
        }

        group[key] = value;
    }

    public new bool RemoveContext(SPWorldKey key)
    {
        // Remove from the main context
        var removed = base.RemoveContext(key);

        // If removed, also remove from the belief group
        if (removed && _beliefGroups.TryGetValue(key.BeliefKey, out var group))
        {
            group.Remove(key);

            if (group.Count == 0)
            {
                _beliefGroups.Remove(key.BeliefKey);
            }
        }

        return removed;
    }

    public new SPBeliefContext Clone()
    {
        var clone = new SPBeliefContext();

        foreach (var kvp in Contexts)
        {
            clone.SetContext(kvp.Key, kvp.Value);
        }

        return clone;
    }

    public Dictionary<SPWorldKey, Datum> GetBeliefs(SPBeliefKey beliefKey)
    {
        return _beliefGroups.TryGetValue(beliefKey, out var group)
            ? group
            : [];
    }

    public Dictionary<SPWorldKey, Datum> GetBeliefs(SPBeliefKey beliefKey, SPInstanceType instanceType)
    {
        if (!_beliefGroups.TryGetValue(beliefKey, out var group))
            return [];

        var result = new Dictionary<SPWorldKey, Datum>(group.Count);

        foreach (var kvp in group)
        {
            if (kvp.Key.InstanceType == instanceType)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    public Dictionary<SPWorldKey, Datum> GetBeliefs(SPBeliefKey beliefKey, SPInstanceType instanceType, ulong instanceId)
    {
        if (!_beliefGroups.TryGetValue(beliefKey, out var group))
            return [];

        var result = new Dictionary<SPWorldKey, Datum>(group.Count);

        foreach (var kvp in group)
        {
            if (kvp.Key.InstanceType == instanceType)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

}