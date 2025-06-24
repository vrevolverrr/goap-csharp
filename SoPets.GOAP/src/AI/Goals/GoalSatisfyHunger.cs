namespace SoPets.GOAP.Goals;

public class GoalSatisfyHunger : SPGoapGoal
{
    public override SPGoalKey GetKey()
    {
        return SPGoalKey.SATISFY_HUNGER;
    }

    public override float GetPriority(SPBeliefContext beliefs)
    {
        return 1;
    }

    public override SPGoapContext<SPWorldKey, SPGoapCondition> GetConditions(SPBeliefContext beliefs)
    {
        var hungerKey = new SPWorldKey(SPBeliefKey.HUNGER);

        if (!beliefs.TryGetContext(hungerKey, out Datum? hungerGoal))
        {
            hungerGoal = new IntDatum(50);
        }

        return new SPGoapContext<SPWorldKey, SPGoapCondition>(
            hungerKey, new SPGoapCondition(hungerKey, new GR_GreaterEqual(hungerGoal + new IntDatum(500))));
    }
}