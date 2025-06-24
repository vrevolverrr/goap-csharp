using SoPets.GOAP.Core;
using SoPets.GOAP.Types;

namespace SoPets.GOAP.Goals;

public class GoalWander : SPGoapGoal
{
    public override SPGoalKey GetKey()
    {
        return SPGoalKey.WANDER;
    }

    public override float GetPriority(SPBeliefContext beliefs)
    {
        return 1;
    }

    public override SPGoapContext<SPWorldKey, SPGoapCondition> GetConditions(SPBeliefContext beliefs)
    {
        var wanderingKey = new SPWorldKey(SPBeliefKey.IS_WANDERING);

        return new SPGoapContext<SPWorldKey, SPGoapCondition>(
            wanderingKey, new SPGoapCondition(wanderingKey, new GR_Equal(Datum.TRUE)));
    }
}