using Microsoft.Extensions.Logging;

namespace SoPets.GOAP;

public abstract class SPGoapAction(ILogger? logger)
{
    protected readonly ILogger? logger = logger;

    public abstract SPActionKey GetKey();

    /// <summary>
    /// Returns the keys of the preconditions that this action requires to be satisfied.
    /// These keys are used as heuristics for preflight checks.
    /// 
    /// IMPORTANT: If these keys are not correctly set, the action may be skipped during planning.
    /// </summary>    
    public abstract SPWorldKey[] PreconditionKeys { get; }

    /// <summary>
    /// Returns the keys of the effects that this action will produce.
    /// These keys are used as heuristics for preflight checks.
    /// 
    /// IMPORTANT: If these keys are not correctly set, the action may be skipped during planning.
    /// </summary>    
    public abstract SPWorldKey[] EffectKeys { get; }

    public abstract SPGoapContext<SPWorldKey, ISPGoapEffect> GetEffects(SPGoapNodeContext context);
    public abstract SPGoapContext<SPWorldKey, SPGoapCondition> GetPreconditions(SPGoapNodeContext context);

    /// <summary>
    /// Takes the current planner node's context and returns a list of settings that can be used to modify the action's behavior.
    /// This is useful for actions that can have different parameters or behaviors based on the context.
    /// </summary>
    /// <param name="context">The current planner node's context.</param>
    /// <returns></returns>
    public abstract List<SPBeliefContext> GetSettings(SPGoapNodeContext context);

    public virtual bool CheckProceduralConditions(SPGoapNodeContext context)
    {
        return true;
    }

    public virtual float GetCost(SPGoapNodeContext context)
    {
        return 1.0f;
    }

    public virtual bool ArePreconditionsSatisfied(SPGoapNodeContext context)
    {
        foreach (var (key, condition) in GetPreconditions(context))
        {
            if (!context.forwardState.TryGetContext(key, out Datum? beliefValue) || beliefValue == null)
            {
                // Key not found in forward state, or belief value is null
                return false;
            }

            if (!condition.IsSatisfied(key, beliefValue))
            {
                // The belief value does not satisfy the precondition
                return false;
            }
        }

        return true;
    }

    public virtual void ApplyEffects(SPGoapNodeContext context)
    {
        foreach (var (_, effect) in GetEffects(context))
        {
            effect.ApplyEffect(context.forwardState);
        }
    }

    public bool IsActionViableFast(SPGoapNodeContext context, bool verbose = false)
    {
        if (verbose) logger?.LogDebug("[{action}] Checking viability of action with context: {context}", GetKey(), context);
        var initialBeliefs = context.initialBeliefs;
        var subgoals = context.subgoals;

        var preconditions = GetPreconditions(context);
        var effects = GetEffects(context);

        if (verbose) logger?.LogDebug("[{action}] Checking precondition conflicts of action with context: {context}", GetKey(), context);
        if (HasPreconditionConflicts(initialBeliefs, subgoals, effects, preconditions))
        {
            if (verbose) logger?.LogDebug("[{action}] Action has precondition conflicts, not viable", GetKey());
            return false; // Action is not viable due to precondition conflicts
        }

        if (HasEffectConflicts(subgoals, effects))
        {
            if (verbose) logger?.LogDebug("[{action}] Action has effect conflicts, not viable", GetKey());
            return false; // Action is not viable due to effect conflicts
        }

        return true;
    }

    public bool HasEffectConflicts(
        SPGoapContext<SPWorldKey, SPGoapCondition> subgoals,
        SPGoapContext<SPWorldKey, ISPGoapEffect> effects,
        bool verbose = false
    )
    {
        foreach (var (key, subgoal) in subgoals)
        {
            if (!effects.TryGetContext(key, out ISPGoapEffect? effect))
            {
                if (verbose) logger?.LogDebug("No effect found for subgoal {subgoal}, skipping conflict checks", subgoal);
                continue; // No effect for this subgoal, skip it
            }

            if (verbose) logger?.LogDebug("Checking effect {effect} for subgoal {subgoal}", effect, subgoal);

            if (CheckEffectConflicts(subgoal, effect, null))
            {
                if (verbose) logger?.LogDebug("Effect {effect} conflicts with subgoal {subgoal}", effect, subgoal);
                return true; // Conflict found
            }
        }

        return false;
    }

    public bool HasPreconditionConflicts(
        SPBeliefContext initialBeliefs,
        SPGoapContext<SPWorldKey, SPGoapCondition> subgoals,
        SPGoapContext<SPWorldKey, ISPGoapEffect> effects,
        SPGoapContext<SPWorldKey, SPGoapCondition> preconditions,
        bool verbose = false)
    {
        foreach (var (key, goal) in subgoals)
        {
            if (!preconditions.TryGetContext(key, out SPGoapCondition precondition))
            {
                // No precondition for this subgoal, skip it
                continue;
            }

            if (verbose) logger?.LogDebug("Checking precondition {precondition} for subgoal {goal}", precondition, goal);

            if (CheckPreconditionConflicts(precondition.requirement, goal.requirement))
            {
                // If the precondition conflicts with the subgoal, check whether the effects resolve the subgoal
                if (!effects.TryGetContext(key, out ISPGoapEffect? effect))
                {
                    if (verbose) logger?.LogDebug("Precondition {precondition} conflicts with subgoal {goal} and no effect resolves it", precondition, goal);
                    return true; // Conflict found

                }
                else
                {
                    // Check if the effect can resolve the subgoal
                    if (!initialBeliefs.TryGetContext(key, out Datum? beliefValue))
                    {
                        // No belief value, conservatively assume effect can resolve it
                        continue;
                    }

                    // Apply the effect to see if it satisfies the subgoal
                    var afterEffect = effect.TestApplyEffect(beliefValue);

                    if (!goal.IsSatisfied(key, afterEffect))
                    {
                        if (verbose) logger?.LogDebug("Precondition {precondition} conflicts with subgoal {goal} and effect {effect} doesn't resolve it", precondition, goal, effect);
                        return true; // Effect doesn't resolve the conflict
                    }

                    // Effect resolves the conflict, continue checking other subgoals
                    if (verbose) logger?.LogDebug("Effect {effect} resolves precondition conflict for subgoal {goal}", effect, goal);
                }

                return true; // Conflict found
            }
        }

        return false;
    }

    //     public bool IsActionViable(SPGoapNodeContext context)
    //     {
    //         if (verbose) Log.Debug($"[{GetKey()}] Checking viability of action with context: {context}");

    //         var subgoals = context.subgoals;
    //         // var forwardState = context.forwardState;

    //         var effects = GetEffects(context);

    //         bool hasProductiveEffect = false;

    //         foreach (var (key, effect) in effects)
    //         {
    //             if (!subgoals.TryGetContext(key, out var subgoal))
    //             {
    //                 // No subgoal for this effect, skip it
    //                 continue;
    //             }

    //             if (verbose) Log.Debug($"Checking effect {key}={effect} for subgoal {subgoal}");

    //             if (!context.initialBeliefs.TryGetContext(key, out Datum? beliefValue))
    //             {
    //                 if (verbose) Log.Debug($"[{GetKey()}] No belief value found for key {key}");
    //             }

    //             // Check for effects that conflict with corresponding subgoal first
    //             if (CheckEffectConflicts(subgoal, effect, beliefValue)) return false;

    //             // Check if the effect is productive for the subgoal, skip subsequent checks
    //             // if we already found a productive effect
    //             if (!hasProductiveEffect && IsEffectProductive(subgoal, effect, beliefValue))
    //             {
    //                 if (verbose) Log.Debug($"[{GetKey()}] Found effect {key}={effect} to be productive for subgoal {subgoal}");
    //                 hasProductiveEffect = true;
    //             }
    //         }

    //         // If no productive effect was found, the action is not viable
    //         if (!hasProductiveEffect) return false;

    //         var preconditions = GetPreconditions(context);

    //         foreach (var (key, condition) in preconditions)
    //         {
    //             if (!subgoals.TryGetContext(key, out SPGoapCondition subgoal))
    //             {
    //                 if (verbose) Log.Debug($"No subgoal found for precondition key: {key}, skipping conflict checks");
    //                 continue; // No subgoal for this precondition, skip it
    //             }

    //             if (verbose) Log.Debug($"[{GetKey()}] Checking precondition conflicts {key}={condition} for subgoal {subgoal}");
    //             if (CheckPreconditionConflicts(condition.requirement, subgoal.requirement)) return false;
    //         }

    //         // Finally, check procedural conditions
    //         if (verbose) Log.Debug($"[{GetKey()}] Checking procedural conditions");
    //         return CheckProceduralConditions(context);
    //     }
    // }

    // public static bool IsEffectProductive(SPGoapCondition subgoal, ISPGoapEffect effect, Datum? beliefValue)
    // {
    //     // conservative assumption: if the forward value is null, the action is productive
    //     if (beliefValue == null) return true;

    //     // Special case for removal effects
    //     if (effect is EF_Remove)
    //     {
    //         Log.Warn($"[{subgoal.key}] Removal effect {effect} used on subgoal {subgoal.key} with belief value {beliefValue}. Is this intended?.");
    //         return false;
    //     }

    //     // TODO DID EF_Remove break this???

    //     // Check if the subgoal is already satisfied
    //     if (subgoal.IsSatisfied(subgoal.key, beliefValue))
    //     {
    //         // If the subgoal is already satisfied, the action cannot be productive
    //         return false;
    //     }

    //     // Apply the effect to the belief value
    //     var afterEffect = effect.TestApplyEffect(beliefValue);

    //     // Check if the effect satisfies the subgoal
    //     if (subgoal.IsSatisfied(subgoal.key, afterEffect))
    //     {
    //         // If the effect satisfies the subgoal, return true
    //         return true;
    //     }

    //     // If the effect does not satisfy the subgoal, check if it improves the distance
    //     float initialDistance = subgoal.CalculateDistance(beliefValue);
    //     float newDistance = subgoal.CalculateDistance(afterEffect);

    //     // If the new distance is less than the initial distance, the effect is productive
    //     return newDistance < initialDistance;
    // }

    /// <summary>
    /// Checks whether the effects of the action undone a subgoal that was previously satisfied.
    /// </summary>
    public bool CheckEffectConflicts(SPGoapCondition subgoal, ISPGoapEffect effect, Datum? beliefValue)
    {
        // conservative assumption: if the belief value is null, assume no conflict
        if (beliefValue == null) return false;

        // Special case for removal effects
        if (effect is EF_Remove)
        {
            logger?.LogWarning("[{subgoal}] Removal effect {effect} used on subgoal {subgoal} with belief value {beliefValue}. Is this intended?.", subgoal.key, effect, subgoal.key, beliefValue);
            return false;
        }

        // TODO: DID EF_Remove break this???

        // Apply the effect to the forward value
        var afterEffect = effect.TestApplyEffect(beliefValue);

        if (subgoal.IsSatisfied(subgoal.key, beliefValue))
        {
            if (!subgoal.IsSatisfied(subgoal.key, afterEffect))
            {
                return true; // Conflict: subgoal was satisfied but now is not
            }

            return false;
        }

        // If the subgoal was not satisfied, there is no conflict since it cannot be undone.
        return false;
    }

    public static bool CheckPreconditionConflicts(ISPGoapRequirement precondition, ISPGoapRequirement subgoal)
    {
        return subgoal.MergeWith(precondition) == null;
    }
}