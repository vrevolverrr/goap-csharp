using SoPets.GOAP.Types;
using SoPets.GOAP.Core;

namespace SoPets.GOAP.Planner;

public readonly struct SPPlanAction(SPActionKey actionKey, SPBeliefContext? actionSettings)
{
    public SPActionKey ActionKey { get; init; } = actionKey;
    public SPBeliefContext? ActionSettings { get; init; } = actionSettings;
}