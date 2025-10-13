using Microsoft.Extensions.Logging;
using Plate.Goap.Adapters;
using Plate.Goap.Core;
using Plate.Goap.Resolver;

namespace WingedBean.Gameplay.Goap;

public class GoapPlannerService : IGoapPlannerService
{
    private readonly ITimeProvider _timeProvider;

    public GoapPlannerService(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public JobResult Plan(IConnectable[] actions, IKeyResolver keyResolver, Func<IGraphResolver, RunData> configure, ILogger? logger = null)
    {
        var resolver = new GraphResolver(actions, keyResolver);
        var runData = configure(resolver);
        var result = resolver.StartResolve(runData).Complete();

        if (logger != null)
        {
            var first = (result.Actions != null && result.Actions.Length > 0) ? result.Actions[0] : null;
            string actionName = "<none>";
            if (first is IGoapAction ga)
                actionName = ga.Config?.Name ?? ga.GetType().Name;
            else if (first is IGoal gg)
                actionName = gg.Config?.Name ?? gg.GetType().Name;
            else if (first != null)
                actionName = first.GetType().Name;
            logger.LogInformation("GOAP plan: chosen first = {Action}", actionName);
        }

        return result;
    }

    public void PlanDemo(ILogger? logger = null)
    {
        // Reuse existing demo runner to avoid duplicating fakes
        GoapDemoRunner.RunOnce(logger);
    }
}
