using SoPets.GOAP.Types;
using SoPets.GOAP.Core;
using SoPets.GOAP.Planner;
using Microsoft.Extensions.Logging;

namespace SoPets.GOAP.Actions;

public class ActionWander(ILogger? logger) : SPGoapAction(logger)
{
    public override SPActionKey GetKey()
    {
        return SPActionKey.WANDER;
    }

    public override SPWorldKey[] PreconditionKeys => [new SPWorldKey(SPBeliefKey.IS_AT_POSITION)];
    public override SPWorldKey[] EffectKeys => [new SPWorldKey(SPBeliefKey.IS_WANDERING)];

    public override SPGoapContext<SPWorldKey, SPGoapCondition> GetPreconditions(SPGoapNodeContext context)
    {
        var settings = context.settings;

        if (settings == null || !settings.Any())
        {
            // If there are no settings, we cannot wander
            return [];
        }

        if (!settings.TryGetContext(new SPWorldKey(SPBeliefKey.TARGET_POSITION), out var targetPosition) || targetPosition == null)
        {
            // If we cannot find the target position in settings, we cannot wander
            return [];
        }

        var key = new SPWorldKey(SPBeliefKey.IS_AT_POSITION);

        // Create a condition that checks if the agent is at the targeted wander position
        return new SPGoapContext<SPWorldKey, SPGoapCondition>(
            key, new SPGoapCondition(key, new GR_Equal(targetPosition)));
    }

    public override SPGoapContext<SPWorldKey, ISPGoapEffect> GetEffects(SPGoapNodeContext context)
    {
        var key = new SPWorldKey(SPBeliefKey.IS_WANDERING);

        return new SPGoapContext<SPWorldKey, ISPGoapEffect>(
            key, new EF_Set { Key = key, Value = Datum.TRUE });
    }


    public override List<SPBeliefContext> GetSettings(SPGoapNodeContext context)
    {
        // TODO: Generate random position for wandering
        Datum randomPosition = Datum.CreateVector2(5, 0);

        var targetKey = new SPWorldKey(SPBeliefKey.TARGET_POSITION);

        return [
            new SPBeliefContext(targetKey, randomPosition)
        ];
    }
}