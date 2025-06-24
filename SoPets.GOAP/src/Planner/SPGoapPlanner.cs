using System.Collections.Immutable;
using System.Text;
using SoPets.GOAP.Core;
using SoPets.GOAP.Types;
using SoPets.GOAP.Actions;
using SoPets.GOAP.Goals;

namespace SoPets.GOAP.Planner;

public static class SPGoapPlanner
{
    static readonly int MAX_NODES_TO_EXPAND = 100;
    static readonly int MAX_INTERATIONS = 1000;

    private static readonly FastPriorityQueue openSet = new(MAX_NODES_TO_EXPAND);
    private static readonly Dictionary<SPBeliefContext, float> costToReachState = [];

    // TODO: ADD PLANNER LEVEL CACHING

    private static readonly Dictionary<SPActionKey, SPGoapAction> _actions = new()
    {
        [SPActionKey.WANDER] = new ActionWander(),
        [SPActionKey.MOVE_TO_TARGET] = new ActionMoveToTarget(),
        [SPActionKey.EAT_FOOD] = new ActionEatFood(),
    };

    private static readonly Dictionary<SPGoalKey, SPGoapGoal> _goals = new()
    {
        [SPGoalKey.WANDER] = new GoalWander(),
        [SPGoalKey.SATISFY_HUNGER] = new GoalSatisfyHunger(),
    };

    public static List<SPGoapAction> GetActionsFromKeys(List<SPActionKey> actionKeys)
    {
        List<SPGoapAction> actions = [];
        foreach (var key in actionKeys)
        {
            if (_actions.TryGetValue(key, out SPGoapAction? action))
            {
                actions.Add(action);
            }
            else
            {
                Log.Error($"Action with key {key} not found in action set.");
            }
        }

        return actions;
    }

    public static SPGoapGoal GetGoalFromKey(SPGoalKey goalKey)
    {
        if (_goals.TryGetValue(goalKey, out SPGoapGoal? goal))
        {
            return goal;
        }

        Log.Error($"Goal with key {goalKey} not found in goal set.");

        throw new KeyNotFoundException($"Goal with key {goalKey} not found.");
    }

    public static List<SPPlanAction> FindPlan(SPGoapAgent agent)
    {
        SPGoapNode start = new(
            parent: null,
            agent: agent,
            nextAction: null,
            settings: null
        );

        (SPGoapNode node, bool isOptimal) = RunSearch(start);

        List<SPPlanAction> planActions = ConstructPlan(node);

        // If the plan is not optimal (i.e. we didn't find a start-to-goal plan).
        // we may need to revise the plan to ensure it is valid.
        if (!isOptimal)
        {
            Log.Info("Plan is not optimal, revising the partial plan.");
            planActions = RevisePartialPlan(agent, planActions);
        }

        // TODO: Post plan reordering

        return planActions;
    }

    private static (SPGoapNode, bool) RunSearch(SPGoapNode start)
    {
        // Reset the open and closed sets for a new search
        openSet.Clear();
        costToReachState.Clear();

        openSet.Enqueue(start, start.GetCost());

        costToReachState[start.GetForwardState()] = 0;

        int iterations = 0;

        // The best effort node is the one that has highest g cost so far (i.e. the longest path found).
        SPGoapNode bestEffortNode = start;
        float minHeuristicSoFar = start.GetHeuristicCost();

        // Main A* search loop
        // TODO: consider using beam search to only consider the best N nodes at each iteration?
        while (openSet.Count > 0 && iterations < MAX_INTERATIONS && openSet.Count + 1 < MAX_NODES_TO_EXPAND)
        {
            SPGoapNode currentNode = openSet.Dequeue();
            var currentState = currentNode.GetForwardState();
            float currentPathCost = currentNode.GetPathCost();

            // Skip if we have already explored this state with a lower cost
            if (costToReachState.TryGetValue(currentState, out float value) && currentPathCost > value)
            {
                if (verbose) Log.Debug($"Skipping node {currentNode} with cost {currentPathCost} as it is not better than existing cost {value}.");
                continue;
            }

            // Update the best effort node if it has a lower heuristic cost
            float currentHeuristic = currentNode.GetHeuristicCost();
            if (currentHeuristic < minHeuristicSoFar)
            {
                minHeuristicSoFar = currentHeuristic;
                bestEffortNode = currentNode;
            }

            if (verbose) Log.Debug($"Expanding node: {currentNode}");
            foreach (var child in currentNode.Expand())
            {
                iterations++;

                // Early exit if we satisfied the goal
                if (child.IsGoalSatisfied())
                {
                    // Found a plan, early exit
                    Log.Info($"Found a plan after {iterations} iterations.");
                    return (child, true);
                }

                SPBeliefContext childState = child.GetForwardState();
                float childPathCost = child.GetPathCost();

                // If we have seen this state before with a cost that is less than or equal
                // to this new path's cost, then this new path is not better. Skip it.
                if (costToReachState.TryGetValue(childState, out float existingCost) && existingCost <= childPathCost)
                {
                    if (verbose) Log.Debug($"Skipping child node {child} with cost {childPathCost} as it is not better than existing cost {existingCost}.");
                    continue; // Found a better or equal path already.
                }

                if (verbose) Log.Debug($"Queuing successor node: {child}");

                float childCost = child.GetCost();
                costToReachState[childState] = childCost;

                // Add the child to the open set
                openSet.Enqueue(child, childCost);

                // stateToNode[childState] = child;
            }
        }

        Log.Info($"Failed to find a optimal plan after {iterations} iterations. Using the best effort plan.");
        return (bestEffortNode, false);
    }

    public static List<SPPlanAction> ConstructPlan(SPGoapNode node)
    {
        List<SPPlanAction> planActions = [];

        SPGoapNode currentNode = node;

        while (currentNode.GetParent() != null)
        {
            SPGoapAction action = currentNode.GetAction()!;

            planActions.Add(new SPPlanAction(
                actionKey: action.GetKey(),
                actionSettings: SPActionSettings.FromContext(currentNode.GetSettings())
            ));

            currentNode = currentNode.GetParent()!;
        }

        return planActions;
    }

    /// <summary>
    /// Peforms a forward validation pass on the partial plan to ensure that all actions can be executed.
    /// Else, it will return the valid actions up until the first action that fails its preconditions.
    /// </summary>
    public static List<SPPlanAction> RevisePartialPlan(SPGoapAgent agent, List<SPPlanAction> planActions)
    {
        SPBeliefContext forwardState = agent.InitialBeliefs.Clone();

        // Create a new working node context
        SPGoapNodeContext nodeContext = new(
            initialBeliefs: agent.InitialBeliefs.Clone(),
            forwardState: forwardState,
            subgoals: agent.Goals,
            settings: null,
            nextAction: null
        );

        for (int i = 0; i < planActions.Count; i++)
        {
            SPPlanAction action = planActions[i];

            SPGoapAction actionInstance = _actions.GetValueOrDefault(action.ActionKey)!;
            nodeContext.settings = action.ActionSettings.ToContext();

            if (actionInstance.ArePreconditionsSatisfied(nodeContext))
            {
                actionInstance.ApplyEffects(nodeContext);
            }
            else
            {
                return planActions[..i];
            }
        }

        // If we reach here, all actions in the plan are valid
        return planActions;
    }

    public static string PrettyPrintPlan(List<SPPlanAction> planActions)
    {
        StringBuilder sb = new();

        foreach (var action in planActions)
        {
            StringBuilder actionSettings = new();

            foreach (var kvp in action.ActionSettings)
            {
                actionSettings.Append($"{kvp.Key.BeliefKey}={kvp.Value}, ");
            }

            sb.Append($"{action.ActionKey}({actionSettings}) -> ");
        }

        return sb.ToString();
    }
}