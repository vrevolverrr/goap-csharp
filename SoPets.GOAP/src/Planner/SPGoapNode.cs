using SoPets.GOAP.Core;
using SoPets.GOAP.Types;
using Microsoft.Extensions.Logging;

namespace SoPets.GOAP.Planner;

/// <summary>
/// Represents a node in the SPGoap planner.
/// A node's main state is the current subgoals that the planner has regressed to.
/// </summary>
public sealed class SPGoapNode
{
    private readonly ILogger? _logger;

    /// <summary>
    /// Flag to enable verbose logging for debugging purposes.
    /// </summary>
    private readonly bool _verbose = false;

    /// <summary>
    /// The current state of the search, which is the set of subgoals that the planner has 
    /// managed to regressed to.
    /// </summary>
    private readonly SPGoapContext<SPWorldKey, SPGoapCondition> _subgoals;

    /// <summary>
    /// The forward state of the search, which is the current predicted beliefs after
    /// evaluating the planned backward actions.
    /// This forward state is used to dynamically evaluate the cost of an action due to the
    /// effects of the actions after itself. 
    /// For example, if the actions after this node in the forward plan has caused the health of the agent
    /// to be low, then the cost of deducting more health in the current action may be higher.
    /// </summary>
    private readonly SPBeliefContext _forwardState;

    /// <summary>
    /// The preconditions of the action that led to this node.
    /// These preconditions are merged with the subgoals of the node.
    /// </summary>
    private readonly SPGoapContext<SPWorldKey, SPGoapCondition>? _preconditions;

    /// <summary>
    /// The effects of the action that led to this node.
    /// These effects are applied to the forward state of the search.
    /// </summary>
    private readonly SPGoapContext<SPWorldKey, ISPGoapEffect>? _effects;

    /// <summary>
    /// The settings for the action that led to this node.
    /// These settings are the parameters that the action can use to modify its behavior.
    /// </summary>
    private readonly SPBeliefContext? _settings;

    /// <summary>
    /// The agent that is performing the planning.
    /// This agent is used to access the initial beliefs, action set, and goals.
    /// </summary>
    private readonly SPGoapAgent _agent;

    /// <summary>
    /// The parent node in the search that led us to this node.
    /// </summary>
    private readonly SPGoapNode? _parent;

    /// <summary>
    /// The action (edge) that brought us to this node.
    /// </summary>
    private readonly SPGoapAction? _action;

    /// <summary>
    /// The actual cost from start node to current node (g).
    /// </summary>
    private readonly float _pathCost = 0;

    /// <summary>
    /// The estimated cost from current node to the target node (h).
    /// </summary>
    private readonly float _heuristicCost = 0;

    /// <summary>
    /// The total estimated cost of cheapest path from start to target through this node (f = g + h).
    /// </summary>
    private readonly float _cost = 0;

    private readonly List<SPGoapNode> _expandList = [];

    /// <summary>
    /// Properties for the priority queue.
    /// </summary>
    public int QueueIndex { get; set; }
    public float Priority { get; set; }

    public SPGoapNode(SPGoapNode? parent,
                      SPGoapAgent agent,
                      SPGoapAction? nextAction,
                      SPBeliefContext? settings,
                      ILogger? logger,
                      bool verbose = false)
    {
        _verbose = verbose;
        _logger = logger;

        _agent = agent;
        _parent = parent;
        _action = nextAction;

        if (settings != null)
        {
            _settings = settings.Clone();
        }

        if (_parent != null)
        {
            _forwardState = _parent.GetForwardState().Clone();
            _subgoals = _parent.GetSubgoals().Clone();
            _pathCost = _parent.GetPathCost();
        }
        else
        {
            _forwardState = _agent.InitialBeliefs.Clone();
            _subgoals = _agent.Goals.Clone();
        }

        if (_action != null)
        {
            // Create a new node context with all the necessary data on the current state of the planner.
            SPGoapNodeContext nodeContext = new(
                initialBeliefs: _agent.InitialBeliefs,
                forwardState: _forwardState,
                subgoals: _subgoals,
                settings: _settings,
                nextAction: _action
            );

            _effects = _action.GetEffects(nodeContext);
            _preconditions = _action.GetPreconditions(nodeContext);

            // Adding the action cost to this node's path cost
            _pathCost += _action.GetCost(nodeContext);

            foreach (var (key, effect) in _effects)
            {
                // Apply effects of the action to the forward state
                // TODO find better way to deal with null keys
                // currently just let the effect deal with it
                effect.ApplyEffect(_forwardState);
                if (_verbose) _logger?.LogDebug("Applied effect {effect} for key {key} to forward state: {forwardState}", effect, key, _forwardState);

                // Regress the subgoals based on the effects of the action
                if (_subgoals.TryGetContext(key, out SPGoapCondition existingSubgoal))
                {
                    // If the subgoal is an EQUAL requirement, we can remove it
                    // because the effect will set the value to the same as the subgoal
                    if (existingSubgoal.requirement is GR_Equal && effect is EF_Set)
                    {
                        // Should already be true, but we check just in case
                        if (existingSubgoal.requirement.IsSatisfied(effect.Value))
                        {
                            _subgoals.RemoveContext(key);
                        }
                        else
                        {
                            _logger?.LogWarning("Subgoal {existingSubgoal} for key {key} is not satisfied by effect {effect}. " +
                                     "This may indicate a design flaw in the action or planner.", existingSubgoal, key, effect);
                        }
                    }
                    else
                    {
                        // Otherwise, we regress the subgoal by applying the effect
                        SPGoapCondition regressedSubgoal = existingSubgoal.ApplyEffect(effect);
                        _subgoals.SetContext(key, regressedSubgoal);
                    }
                }
            }

            // Merge the current subgoals with the preconditions of the action
            foreach (var (key, precondition) in _preconditions)
            {
                // If the precondition is not already in the subgoals, add it
                if (!_subgoals.HasContext(key))
                {
                    _subgoals.SetContext(key, precondition);
                }
                else
                {
                    if (_subgoals.TryGetContext(key, out SPGoapCondition existingSubgoal))
                    {
                        // If it exists, we merge the existing subgoal with the precondition
                        SPGoapCondition? mergedSubgoal = existingSubgoal.MergeWith(precondition);

                        // The merge should not return null, but we check just in case
                        if (mergedSubgoal != null)
                        {
                            _subgoals.SetContext(key, mergedSubgoal.Value);
                        }
                        else
                        {
                            _logger?.LogWarning("Failed to merge subgoal {existingSubgoal} with precondition {precondition} for key {key}. " +
                                    "This may indicate a design flaw in the action or planner.", existingSubgoal, precondition, key);
                        }
                    }
                }
            }
        }

        _heuristicCost = CalculateHeuristicCost(_subgoals, _agent.InitialBeliefs);
        _cost = _pathCost + _heuristicCost;
    }

    /// <summary>
    /// Checks if the current node's subgoals are all satisfied.
    ///  If all subgoals are satisfied, the planner can stop searching for a path.
    /// </summary>
    /// <returns></returns>
    public bool IsGoalSatisfied()
    {
        // Check if all subgoals are satisfied
        foreach (var (key, requirement) in _subgoals)
        {
            // If the belief value is not found or is null, the requirement cannot be satisfied
            if (!_agent.InitialBeliefs.TryGetContext(key, out Datum? beliefValue) || beliefValue == null)
            {
                return false;
            }

            // Check if the requirement is satisfied by the belief value
            if (!requirement.IsSatisfied(key, beliefValue))
            {
                return false;
            }
        }

        return true;
    }

    public List<SPGoapNode> Expand()
    {
        // Clear the expand list in case the reference was modified
        _expandList.Clear();

        SPGoapNodeContext nodeContext = new(
            initialBeliefs: _agent.InitialBeliefs,
            forwardState: _forwardState,
            settings: null,
            subgoals: _subgoals,
            nextAction: _action
        );

        List<SPGoapAction> actions = _agent.ActionSet;

        for (var i = 0; i < actions.Count; i++)
        {
            SPGoapAction possibleAction = actions[i];

            // Preflight check for action viability
            if (!_subgoals.HasAny(possibleAction.EffectKeys))
            {
                // If the action's effects are already satisfied by the subgoals, skip this action.
                if (_verbose) _logger?.LogDebug("Skipping action {actionKey} as its effects are not relevant to the subgoals.", possibleAction.GetKey());
                continue;
            }

            if (_verbose) _logger?.LogDebug("Generating possible settings for action: {actionKey}", possibleAction.GetKey());
            List<SPBeliefContext> possibleSettings = possibleAction.GetSettings(nodeContext);

            if (possibleSettings.Count == 0)
            {
                // There are no ways this action can run with the current context,
                // skip this action.
                continue;
            }

            if (_verbose) _logger?.LogDebug("Found {count} possible settings for action: {actionKey}", possibleSettings.Count, possibleAction.GetKey());

            foreach (var setting in possibleSettings)
            {
                nodeContext.settings = setting;

                // Check whether action is viable
                if (possibleAction.IsActionViableFast(nodeContext, verbose: _verbose))
                {
                    var newNode = new SPGoapNode(
                        agent: _agent,
                        settings: setting,
                        parent: this,
                        logger: _logger,
                        nextAction: possibleAction
                    );

                    // Add the new node to the expand list
                    if (_verbose) _logger?.LogDebug("Found successor node: {newNode} through action {actionKey} with settings {settings}", newNode, possibleAction.GetKey(), setting);
                    _expandList.Add(newNode);
                }
                else
                {
                    if (_verbose) _logger?.LogDebug("Action {actionKey} with settings {settings} is not viable, skipping.", possibleAction.GetKey(), setting);
                }
            }
        }

        if (_verbose) _logger?.LogDebug("Found {count} successor nodes.", _expandList.Count);
        return _expandList;
    }

    /// <summary>
    /// Gets the current subgoals that are yet to be achieved at this node.
    /// </summary>
    /// <returns>A `SPGoapContext` instance containing keys for the requirements for the subgoals</returns>
    public SPGoapContext<SPWorldKey, SPGoapCondition> GetSubgoals()
    {
        return _subgoals;
    }

    /// <summary>
    ///  Gets the forward state of the search.
    /// This is the predicted beliefs after evaluating the planned backward actions.
    /// </summary>
    /// <returns>A `SPGoapContext` instance containing the keys for the values of the beliefs after applying
    /// the effects of the planned backward actions.
    /// </returns>
    public SPBeliefContext GetForwardState()
    {
        return _forwardState;
    }

    /// <summary>
    /// Gets the parent node in the search graph.
    /// This is the node that led to this node in the search process.
    /// </summary>
    /// <returns>The parent node, or null if this node is the start node.</returns>
    public SPGoapNode? GetParent()
    {
        return _parent;
    }

    public SPBeliefContext? GetSettings()
    {
        return _settings;
    }

    /// <summary>
    /// Gets the action that led to this node.
    /// This action is the edge in the search graph that connects the parent node to this node.
    /// </summary>
    /// <returns>The action that led to this node, or null if this node is the start node.</returns>
    public SPGoapAction? GetAction()
    {
        return _action;
    }

    /// <summary>
    /// Gets the path cost from the start node to this node.
    /// This is the actual cost (g) accumulated from the start node to this node.
    /// </summary>
    /// <returns><c>g</c>, The path cost from the start node to this node.</returns>
    public float GetPathCost()
    {
        return _pathCost;
    }

    /// <summary>
    /// Gets the heuristic cost from this node to the target node.
    /// This is the estimated cost (h) to reach the target node from this node.
    /// In the context of SPGoap, this is the estimated cost to satisfy the remaining subgoals.
    /// </summary>
    /// <returns><c>h</c>, The heuristic cost to reach the target node.</returns>
    public float GetHeuristicCost()
    {
        return _heuristicCost;
    }

    /// <summary>
    /// Gets the total estimated cost of the cheapest path from the start node to the target node through this node.
    /// This is represented by `f = g + h`.
    /// </summary>
    /// <returns><c>f</c>, The total estimated cost of the path through this node.</returns>
    public float GetCost()
    {
        return _cost;
    }

    public override string ToString()
    {
        return
        $"""
        SPGoapNode (HashCode: {GetHashCode()}):
        Action: {_action?.GetKey().ToString() ?? "None"}
        Costs: g={_pathCost}, h={_heuristicCost}, f={_cost}
        Subgoals: {_subgoals}
        ForwardState: {_forwardState}
        """;
    }

    /// <summary>
    ///  Calculates the heuristic cost for the current subgoals
    ///  based on the initial beliefs.
    /// </summary>
    /// <param name="subgoals">The current subgoals</param>
    /// <param name="initialBeliefs">The initial beliefs</param>
    /// <returns></returns>
    public static float CalculateHeuristicCost(SPGoapContext<SPWorldKey, SPGoapCondition> subgoals, SPBeliefContext initialBeliefs)
    {
        float h = 0;

        foreach (var (key, condition) in subgoals)
        {
            if (!initialBeliefs.TryGetContext(key, out Datum? beliefValue) || beliefValue == null)
            {
                // If the belief value is null or missing, we apply a penalty
                h += condition.MissingBeliefPenalty;
                continue;
            }

            h += condition.CalculateDistance(beliefValue);
        }

        return h;
    }
}

