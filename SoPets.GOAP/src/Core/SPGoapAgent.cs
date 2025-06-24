using SoPets.GOAP.Types;

namespace SoPets.GOAP.Core;

public class SPGoapAgent(SPBeliefContext initialBeliefs, List<SPGoapAction> actionSet, SPGoapContext<SPWorldKey, SPGoapCondition> goals)
{
    public SPGoapContext<SPWorldKey, SPGoapCondition> Goals { get; init; } = goals;
    public List<SPGoapAction> ActionSet { get; init; } = [.. actionSet];
    public SPBeliefContext InitialBeliefs { get; init; } = initialBeliefs.Clone();
}