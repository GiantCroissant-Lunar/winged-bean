using Microsoft.Extensions.Logging;
using Plate.Goap.Core;
using Plate.Goap.Resolver;

namespace WingedBean.Gameplay.Goap;

public interface IGoapPlannerService
{
    JobResult Plan(IConnectable[] actions, IKeyResolver keyResolver, Func<IGraphResolver, RunData> configure, ILogger? logger = null);
    void PlanDemo(ILogger? logger = null);
}
