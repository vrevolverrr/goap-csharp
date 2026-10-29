using Microsoft.Extensions.Logging;
using SoPets.GOAP;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

SPGoapPlanner.SetLogger(loggerFactory.CreateLogger("SoPets.GOAP"));

SPBeliefContext initialBeliefs = new(new Dictionary<SPWorldKey, Datum>
{
    [new SPWorldKey(SPBeliefKey.HUNGER)] = new IntDatum(0),
    [new SPWorldKey(SPBeliefKey.IS_AT_POSITION)] = Datum.CreateVector2(0, 0),
    [new SPWorldKey(SPBeliefKey.SAW_ITEM, SPInstanceType.FOOD, 1)] = Datum.CreateVector2(1, 0),
    [new SPWorldKey(SPBeliefKey.SAW_ITEM, SPInstanceType.FOOD, 2)] = Datum.CreateVector2(2, 0),
    [new SPWorldKey(SPBeliefKey.SAW_ITEM, SPInstanceType.FOOD, 3)] = Datum.CreateVector2(3, 0),
});

SPGoapAgent agent = new(
    initialBeliefs: initialBeliefs,
    actionSet: SPGoapPlanner.GetActionsFromKeys([
        SPActionKey.EAT_FOOD,
        SPActionKey.MOVE_TO_TARGET,
        SPActionKey.WANDER,
    ]),
    goals: SPGoapPlanner.GetGoalFromKey(SPGoalKey.SATISFY_HUNGER).GetConditions(initialBeliefs)
);

var plan = SPGoapPlanner.FindPlan(agent);

Console.WriteLine(SPGoapPlanner.PrettyPrintPlan(plan));

// Visiting food in order of proximity from (0, 0) walks 3 units, farthest-first would walk 5.
Datum at = Datum.CreateVector2(0, 0);
float walked = 0;
foreach (var action in plan.Where(a => a.ActionKey == SPActionKey.MOVE_TO_TARGET))
{
    action.ActionSettings!.TryGetContext(new SPWorldKey(SPBeliefKey.TARGET_POSITION), out Datum? target);
    walked += at.DistanceTo(target!);
    at = target!;
}

Console.WriteLine($"Walked {walked} units");
if (walked != 3) throw new InvalidOperationException($"Expected a walk of 3 units, got {walked}.");
