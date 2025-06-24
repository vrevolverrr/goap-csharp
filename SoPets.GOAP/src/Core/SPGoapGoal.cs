using SoPets.GOAP.Types;

namespace SoPets.GOAP.Core;

public abstract class SPGoapGoal
{
    protected SPGoapContext<SPWorldKey, SPGoapCondition> conditions = new();

    public abstract SPGoalKey GetKey();

    public abstract SPGoapContext<SPWorldKey, SPGoapCondition> GetConditions(SPBeliefContext beliefs);

    public abstract float GetPriority(SPBeliefContext beliefs);

}