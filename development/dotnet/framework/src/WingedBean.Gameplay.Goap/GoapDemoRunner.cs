using System;
using Microsoft.Extensions.Logging;
using Plate.Agent.Core;
using Plate.Goap.Adapters;
using Plate.Goap.Core;
using Plate.Goap.Resolver;

namespace WingedBean.Gameplay.Goap;

public static class GoapDemoRunner
{
    public static void RunOnce(Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        try
        {
            var keyResolver = new FakeKeyResolver();

            var goal = new FakeGoal("ReachA", new[] { new FakeCondition("A") });
            var actionNear = new FakeAction("ActNear", new[] { new FakeEffect("A") });
            var actionFar = new FakeAction("ActFar", new[] { new FakeEffect("A") });

            var resolver = new GraphResolver(new IConnectable[] { goal, actionNear, actionFar }, keyResolver);

            var enabled = resolver.GetEnabledBuilder().Build();
            var executableBuilder = resolver.GetExecutableBuilder();
            executableBuilder.SetExecutable(actionNear, true);
            executableBuilder.SetExecutable(actionFar, true);
            var executable = executableBuilder.Build();

            var positionBuilder = resolver.GetPositionBuilder();
            positionBuilder.SetPosition(goal, new Position3D(0, 0, 0));
            positionBuilder.SetPosition(actionNear, new Position3D(1, 0, 0));
            positionBuilder.SetPosition(actionFar, new Position3D(10, 0, 0));
            var positions = positionBuilder.Build();

            var costBuilder = resolver.GetCostBuilder();
            costBuilder.SetCost(actionNear, 5f);
            costBuilder.SetCost(actionFar, 1f);
            var costs = costBuilder.Build();

            var conditionBuilder = resolver.GetConditionBuilder();
            conditionBuilder.SetConditionMet(goal.Conditions[0], false);
            var conditionsMet = conditionBuilder.Build();

            var runData = new RunData
            {
                StartIndex = resolver.GetIndex(goal),
                IsEnabled = enabled,
                IsExecutable = executable,
                Positions = positions,
                Costs = costs,
                ConditionsMet = conditionsMet,
                DistanceMultiplier = 1.0f,
            };

            var result = resolver.StartResolve(runData).Complete();
            var chosen = (result.Actions != null && result.Actions.Length > 0) ? result.Actions[0] : null;

            var actionName = "<none>";
            if (chosen is IGoapAction ga)
                actionName = ga.Config?.Name ?? ga.GetType().Name;
            else if (chosen is IGoal gg)
                actionName = gg.Config?.Name ?? gg.GetType().Name;
            else if (chosen != null)
                actionName = chosen.GetType().Name;

            if (logger != null)
                logger.LogInformation("GOAP demo result: Chosen action = {Action}", actionName);
            else
                Console.WriteLine($"GOAP demo result: Chosen action = {actionName}");
        }
        catch (Exception ex)
        {
            if (logger != null)
                logger.LogError(ex, "GOAP demo run failed");
            else
                Console.WriteLine($"GOAP demo run failed: {ex}");
        }
    }

    // Fakes (minimal)
    private class FakeKeyResolver : IKeyResolver
    {
        public string GetKey(ICondition condition) => condition.WorldKey.Name;
        public string GetKey(IEffect effect) => effect.WorldKey.Name;
        public bool AreConflicting(IEffect effect, ICondition condition) => false;
        public void SetWorldData(IWorldData globalWorldData) { }
    }

    private class FakeWorldKey : IWorldKey
    {
        public FakeWorldKey(string name) { Name = name; }
        public string Name { get; }
    }

    private class FakeCondition : ICondition
    {
        public FakeCondition(string key) { WorldKey = new FakeWorldKey(key); }
        public IWorldKey WorldKey { get; }
        public Comparison Comparison { get; } = Comparison.SmallerThan;
        public int Amount { get; } = 1;
    }

    private class FakeEffect : IEffect
    {
        public FakeEffect(string key) { WorldKey = new FakeWorldKey(key); }
        public IWorldKey WorldKey { get; }
        public bool Increase { get; } = true;
    }

    private class FakeGoalConfig : IGoalConfig
    {
        public string Name { get; set; } = "FakeGoal";
        public string ClassType { get; set; } = typeof(FakeGoal).FullName!;
        public float BaseCost { get; set; } = 0f;
        public System.Collections.Generic.List<ICondition> Conditions { get; } = new();
        public void Validate() { }
    }

    private class FakeActionConfig : IActionConfig
    {
        public string Name { get; set; } = "FakeAction";
        public string ClassType { get; set; } = typeof(FakeAction).FullName!;
        public float BaseCost { get; set; } = 0f;
        public ITargetKey Target { get; } = new FakeTargetKey();
        public float StoppingDistance { get; } = 0f;
        public bool ValidateTarget { get; } = false;
        public bool RequiresTarget { get; } = false;
        public bool ValidateConditions { get; } = false;
        public ICondition[] Conditions { get; } = Array.Empty<ICondition>();
        public IEffect[] Effects { get; } = Array.Empty<IEffect>();
        public ActionMoveMode MoveMode { get; } = ActionMoveMode.PerformWhileMoving;
        public IActionProperties Properties { get; } = new FakeActionProperties();
        public void Validate() { }
    }

    private class FakeTargetKey : ITargetKey
    {
        public string Name { get; } = "Target";
    }

    private class FakeGoal : IGoal
    {
        public FakeGoal(string name, ICondition[] conditions)
        {
            Guid = Guid.NewGuid();
            Config = new FakeGoalConfig { Name = name };
            Conditions = conditions;
            Effects = Array.Empty<IEffect>();
        }

        public Guid Guid { get; }
        public IEffect[] Effects { get; }
        public ICondition[] Conditions { get; }
        public IGoalConfig Config { get; private set; }
        public int Index { get; set; }
        public float GetCost(IActionReceiver agent, IComponentReference references) => 0f;
        public void SetConfig(IGoalConfig config) => Config = config;
    }

    private class FakeAction : IGoapAction
    {
        public FakeAction(string name, IEffect[] effects)
        {
            Guid = Guid.NewGuid();
            Config = new FakeActionConfig { Name = name };
            Effects = effects;
            Conditions = Array.Empty<ICondition>();
        }

        public Guid Guid { get; }
        public IEffect[] Effects { get; }
        public ICondition[] Conditions { get; }
        public IActionConfig Config { get; private set; }

        public ActionMoveMode GetMoveMode(IMonoAgent agent) => ActionMoveMode.PerformWhileMoving;
        public float GetStoppingDistance() => 0f;
        public bool IsInRange(IMonoAgent agent, float distance, IActionData data, IComponentReference references) => true;
        public IActionData GetData() => new FakeActionData();
        public void Created() { }
        public bool IsValid(IActionReceiver agent, IActionData data) => true;
        public void Start(IMonoAgent agent, IActionData data) { }
        public void BeforePerform(IMonoAgent agent, IActionData data) { }
        public IActionRunState? Perform(IMonoAgent agent, IActionData data, IActionContext context) => null;
        public void Stop(IMonoAgent agent, IActionData data) { }
        public void Complete(IMonoAgent agent, IActionData data) { }
        public bool IsExecutable(IActionReceiver agent, bool conditionsMet) => true;
        public float GetCost(IActionReceiver agent, IComponentReference references, ITarget target) => 0f;
        public bool IsEnabled(IActionReceiver agent) => true;
        public void Enable(IActionReceiver receiver) { }
        public void Disable(IActionReceiver receiver, IActionDisabler disabler) { }
        public void SetConfig(IActionConfig config) => Config = config;
    }

    private class FakeActionData : IActionData
    {
        public ITarget Target { get; set; } = default!;
    }

    private class FakeActionProperties : IActionProperties { }
}
