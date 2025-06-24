namespace SoPets.GOAP;

public class SPGoapNodeContext(
    SPBeliefContext initialBeliefs,
    SPBeliefContext forwardState,
    SPGoapContext<SPWorldKey, SPGoapCondition> subgoals,
    SPBeliefContext? settings,
    SPGoapAction? nextAction)
{
    public SPBeliefContext initialBeliefs = initialBeliefs;
    public SPBeliefContext forwardState = forwardState;
    public SPGoapContext<SPWorldKey, SPGoapCondition> subgoals = subgoals;
    public SPBeliefContext? settings = settings;
    public SPGoapAction? nextAction = nextAction;

    public override string ToString()
    {
        return $"""
        SPGoapNodeContext:
        initialBeliefs: {initialBeliefs}
        forwardState: {forwardState}
        subgoals: {subgoals}
        settings: {settings}
        nextAction: {nextAction?.GetKey().ToString() ?? "None"}
        """;
    }
}