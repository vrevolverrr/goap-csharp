using SoPets.GOAP.Types;
using SoPets.GOAP.Core;
using SoPets.GOAP.Planner;
using Microsoft.Extensions.Logging;

namespace SoPets.GOAP.Actions;

public class ActionEatFood(ILogger? logger) : SPGoapAction(logger)
{
    public override SPActionKey GetKey()
    {
        return SPActionKey.EAT_FOOD;
    }

    public override SPWorldKey[] PreconditionKeys => [new SPWorldKey(SPBeliefKey.IS_AT_POSITION)];
    public override SPWorldKey[] EffectKeys => [new SPWorldKey(SPBeliefKey.HUNGER)];

    public override float GetCost(SPGoapNodeContext context)
    {
        var settings = context.settings;

        if (settings == null || !settings.Any()) return float.MaxValue;
        Dictionary<SPWorldKey, Datum> targetFood = settings.GetBeliefs(SPBeliefKey.TARGET_FOOD);

        if (targetFood.Count == 0) return float.MaxValue;

        // Should only have one target food item, but handle multiple for safety
        var targetFoodKey = targetFood.First().Key;

        // Should only have one target food item, but handle multiple for safety
        if (settings.TryGetContext(targetFoodKey, out var foodPosition))
        {
            if (!context.forwardState.TryGetContext(new SPWorldKey(SPBeliefKey.IS_AT_POSITION), out Datum? currentPosition) || currentPosition == null)
            {
                // If we cannot find the current position in forward state, we cannot move
                return float.MaxValue;
            }

            var distance = foodPosition.DistanceTo(currentPosition);
            if (distance < 0.1f) return 0;

            return distance;
        }

        return float.MaxValue;
    }

    public override SPGoapContext<SPWorldKey, SPGoapCondition> GetPreconditions(SPGoapNodeContext context)
    {
        var settings = context.settings;

        if (settings == null || !settings.Any()) return [];
        Dictionary<SPWorldKey, Datum> targetFood = settings.GetBeliefs(SPBeliefKey.TARGET_FOOD);

        if (targetFood.Count == 0) return [];

        // Should only have one target food item, but handle multiple for safety
        var targetFoodKey = targetFood.First().Key;

        // Should only have one target food item, but handle multiple for safety
        if (settings.TryGetContext(targetFoodKey, out var foodPosition))
        {
            // We have a target food item to eat
            var key = new SPWorldKey(SPBeliefKey.IS_AT_POSITION);
            // Set the precondition that we need to be at the position of the food item
            return new SPGoapContext<SPWorldKey, SPGoapCondition>(
                key, new SPGoapCondition(key, new GR_Equal(foodPosition!)));
        }

        return [];
    }

    public override SPGoapContext<SPWorldKey, ISPGoapEffect> GetEffects(SPGoapNodeContext context)
    {
        var settings = context.settings;
        if (settings == null || !settings.Any()) return [];

        Dictionary<SPWorldKey, Datum> targetFood = settings.GetBeliefs(SPBeliefKey.TARGET_FOOD);
        if (targetFood.Count == 0) return [];

        // Should only have one target food item, but handle multiple for safety
        var targetFoodKey = targetFood.First().Key;

        if (settings.TryGetContext(targetFoodKey, out var _))
        {
            var effects = new SPGoapContext<SPWorldKey, ISPGoapEffect>();

            // TODO: Query the food item's hunger bonus, assume fixed hunger first
            Datum hungerBonus = new IntDatum(10);
            SPWorldKey hungerKey = new(SPBeliefKey.HUNGER);
            ISPGoapEffect effect = new EF_Increment { Key = hungerKey, Value = hungerBonus };

            effects.SetContext(hungerKey, effect);

            // Remove the target food item from the beliefs
            SPWorldKey sawTargetKey = new(SPBeliefKey.SAW_ITEM, targetFoodKey.InstanceType, targetFoodKey.InstanceId);
            effects.SetContext(sawTargetKey, new EF_Remove { Key = sawTargetKey });

            return effects;
        }

        return [];
    }

    public override List<SPBeliefContext> GetSettings(SPGoapNodeContext context)
    {
        // TODO need to index instancetypes/beliefkeys to get O(1) access
        if (context.forwardState == null || !context.forwardState.Any()) return [];

        // Get all food items that the agent has seen in the forward state, 
        // taking into account the other actions that might have been performed
        Dictionary<SPWorldKey, Datum> availableFoodContext =
            context.forwardState.GetBeliefs(SPBeliefKey.SAW_ITEM, SPInstanceType.FOOD);

        List<SPBeliefContext> settings = [];

        foreach (var (key, datum) in availableFoodContext)
        {
            SPBeliefContext newSettingsContext = new();
            // Sets the food item as the target food
            SPWorldKey targetFoodKey = new(SPBeliefKey.TARGET_FOOD, SPInstanceType.FOOD, key.InstanceId);
            newSettingsContext.SetContext(targetFoodKey, datum);

            settings.Add(newSettingsContext);
            // TODO DEAL WITH THIS, RETURN CLOSEST FOOD ITEM FIRST THEN RELY ON THE PLANNER TO REORDER AFTERWARDS
        }

        return settings;
    }
}