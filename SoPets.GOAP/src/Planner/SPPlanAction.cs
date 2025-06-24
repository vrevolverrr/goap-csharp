namespace SoPets.GOAP;

public readonly struct SPPlanAction(SPActionKey actionKey, SPBeliefContext? actionSettings)
{
    public SPActionKey ActionKey { get; init; } = actionKey;
    public SPBeliefContext? ActionSettings { get; init; } = actionSettings;
}