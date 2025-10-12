using System.Threading;
using System.Threading.Tasks;

namespace WingedBean.Contracts.Gameplay;

/// <summary>
/// Combat interactions between entities.
/// </summary>
public interface ICombatService
{
    Task<CombatResult> AttackAsync(string attackerId, string targetId, AttackOptions options, CancellationToken ct = default);
}

public sealed class AttackOptions
{
    public string? SkillId { get; set; }
    public int? BaseDamage { get; set; }
}

public sealed class CombatResult
{
    public bool Hit { get; set; }
    public int Damage { get; set; }
    public bool Crit { get; set; }
    public bool TargetDefeated { get; set; }
}
