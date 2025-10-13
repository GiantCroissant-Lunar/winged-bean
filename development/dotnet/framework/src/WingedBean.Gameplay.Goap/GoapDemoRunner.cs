using System;
using Microsoft.Extensions.Logging;
using Plate.Goap.Adapters;
using Plate.Goap.Core;
using Plate.Goap.Resolver;

namespace WingedBean.Gameplay.Goap;

public static class GoapDemoRunner
{
    public static void RunOnce(ILogger? logger = null)
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

            if (logger != null)
                logger.LogInformation("GOAP demo result: Chosen action = {Action}", chosen?.Config?.Name ?? "<none>");
            else
                Console.WriteLine($"GOAP demo result: Chosen action = {chosen?.Config?.Name ?? "<none>"}");
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
        public Plate.Agent.Core.ActionMoveMode MoveMode { get; } = Plate.Agent.Core.ActionMoveMode.PerformWhileMoving;
        public Plate.Agent.Core.IActionProperties Properties { get; } = new FakeActionProperties();
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
        public float GetCost(Plate.Agent.Core.IActionReceiver agent, Plate.Agent.Core.IComponentReference references) => 0f;
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

        public Plate.Agent.Core.ActionMoveMode GetMoveMode(Plate.Agent.Core.IMonoAgent agent) => Plate.Agent.Core.ActionMoveMode.PerformWhileMoving;
        public float GetStoppingDistance() => 0f;
        public bool IsInRange(Plate.Agent.Core.IMonoAgent agent, float distance, Plate.Agent.Core.IActionData data, Plate.Agent.Core.IComponentReference references) => true;
        public Plate.Agent.Core.IActionData GetData() => new FakeActionData();
        public void Created() { }
        public bool IsValid(Plate.Agent.Core.IActionReceiver agent, Plate.Agent.Core.IActionData data) => true;
        public void Start(Plate.Agent.Core.IMonoAgent agent, Plate.Agent.Core.IActionData data) { }
        public void BeforePerform(Plate.Agent.Core.IMonoAgent agent, Plate.Agent.Core.IActionData data) { }
        public Plate.Agent.Core.IActionRunState? Perform(Plate.Agent.Core.IMonoAgent agent, Plate.Agent.Core.IActionData data, Plate.Agent.Core.IActionContext context) => null;
        public void Stop(Plate.Agent.Core.IMonoAgent agent, Plate.Agent.Core.IActionData data) { }
        public void Complete(Plate.Agent.Core.IMonoAgent agent, Plate.Agent.Core.IActionData data) { }
        public bool IsExecutable(Plate.Agent.Core.IActionReceiver agent, bool conditionsMet) => true;
        public float GetCost(Plate.Agent.Core.IActionReceiver agent, Plate.Agent.Core.IComponentReference references, Plate.Agent.Core.ITarget target) => 0f;
        public bool IsEnabled(Plate.Agent.Core.IActionReceiver agent) => true;
        public void Enable(Plate.Agent.Core.IActionReceiver receiver) { }
        public void Disable(Plate.Agent.Core.IActionReceiver receiver, Plate.Agent.Core.IActionDisabler disabler) { }
        public void SetConfig(IActionConfig config) => Config = config;
    }

    private class FakeActionData : Plate.Agent.Core.IActionData
    {
        public Plate.Agent.Core.ITarget Target { get; set; } = default!;
    }

    private class FakeActionProperties : Plate.Agent.Core.IActionProperties { }
}
