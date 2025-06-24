using Microsoft.Extensions.Logging;

namespace SoPets.GOAP;

public class ActionMoveToTarget(ILogger? logger) : SPGoapAction(logger)
{
    public static float COST_SCALING_FACTOR = 1.0f;

    public override SPActionKey GetKey()
    {
        return SPActionKey.MOVE_TO_TARGET;
    }

    public override SPWorldKey[] PreconditionKeys => [];
    public override SPWorldKey[] EffectKeys => [new SPWorldKey(SPBeliefKey.IS_AT_POSITION)];

    public override SPGoapContext<SPWorldKey, SPGoapCondition> GetPreconditions(SPGoapNodeContext context)
    {
        // No preconditions for moving to a target
        return [];
    }

    public override SPGoapContext<SPWorldKey, ISPGoapEffect> GetEffects(SPGoapNodeContext context)
    {
        var settings = context.settings;
        if (settings == null || !settings.Any()) return [];

        if (!settings.TryGetContext(new SPWorldKey(SPBeliefKey.TARGET_POSITION), out Datum? targetPosition) || targetPosition == null)
        {
            // If we cannot find the target position in settings, we don't know where to move
            return [];
        }

        // If we have a target position, we can set the belief that we are at that position
        var key = new SPWorldKey(SPBeliefKey.IS_AT_POSITION);

        return new SPGoapContext<SPWorldKey, ISPGoapEffect>(
            key, new EF_Set { Key = key, Value = targetPosition });
    }

    public override List<SPBeliefContext> GetSettings(SPGoapNodeContext context)
    {
        // Look at where the planner wants to go
        if (!context.subgoals.TryGetContext(new SPWorldKey(SPBeliefKey.IS_AT_POSITION), out SPGoapCondition targetPosition))
        {
            // If there is no target position in the subgoals, we don't have anywhere to go
            return [];
        }

        // If we have a target position, we can create a setting for it
        // Create new settings context with the target position
        var key = new SPWorldKey(SPBeliefKey.TARGET_POSITION);
        return [new SPBeliefContext(key, targetPosition.requirement.Value)];
    }

    public override float GetCost(SPGoapNodeContext context)
    {
        return 1.0f;
        // var settings = context.settings;

        // if (settings == null || !settings.Any()) return float.MaxValue;

        // // Get the current position and target position from the context
        // if (!settings.TryGetContext(new SPWorldKey(SPBeliefKey.TARGET_POSITION), out Datum? targetPosition) || targetPosition == null)
        // {
        //     // If we cannot find the target position in settings, we cannot move
        //     return float.MaxValue;
        // }

        // if (!context.forwardState.TryGetContext(new SPWorldKey(SPBeliefKey.IS_AT_POSITION), out Datum? currentPosition) || currentPosition == null)
        // {
        //     // If we cannot find the current position in forward state, we cannot move
        //     return float.MaxValue;
        // }

        // // Estimate the cost to move to the target position
        // float cost = targetPosition.DistanceTo(currentPosition) * COST_SCALING_FACTOR;

        // return cost;
    }
}