
namespace SoPets.GOAP.Planner;

[Type]
public readonly partial struct SPPlanAction(SPActionKey actionKey, SPActionSettings actionSettings)
{
    public SPActionKey ActionKey { get; init; } = actionKey;
    public SPActionSettings ActionSettings { get; init; } = actionSettings;
}