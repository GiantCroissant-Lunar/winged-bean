using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WingedBean.Contracts.Gameplay;

/// <summary>
/// Quest state management.
/// </summary>
public interface IQuestService
{
    Task<IReadOnlyList<QuestState>> GetQuestsAsync(string entityId, CancellationToken ct = default);
    Task<bool> StartQuestAsync(string entityId, string questId, CancellationToken ct = default);
    Task<bool> CompleteQuestAsync(string entityId, string questId, CancellationToken ct = default);
}

public sealed class QuestState
{
    public string QuestId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public QuestStatus Status { get; set; }
}

public enum QuestStatus { NotStarted, InProgress, Completed }
