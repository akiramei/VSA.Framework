using VSA.Kernel;

namespace Domain.Library.Members;

/// <summary>
/// 利用者（集約ルート）
///
/// 図書館の利用者を管理する。
/// </summary>
public sealed class Member : AggregateRoot<MemberId>
{
    public string Name { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public MemberStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Member() { } // EF Core用

    /// <summary>
    /// 利用者を作成
    /// </summary>
    public static Member Create(MemberId id, string name, string? email = null, string? phone = null)
    {
        return new Member
        {
            Id = id,
            Name = name,
            Email = email,
            Phone = phone,
            Status = MemberStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ================================================================
    // Boundaryメソッド（操作可否判定）
    // ================================================================

    /// <summary>
    /// 貸出可否を判定
    /// </summary>
    public BoundaryDecision CanBorrow()
    {
        if (Status == MemberStatus.Suspended)
            return BoundaryDecision.Deny("貸出停止中のため借りることができません");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 予約可否を判定
    /// </summary>
    public BoundaryDecision CanReserve()
    {
        if (Status == MemberStatus.Suspended)
            return BoundaryDecision.Deny("貸出停止中のため予約できません");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 停止可否を判定
    /// </summary>
    public BoundaryDecision CanSuspend()
    {
        if (Status == MemberStatus.Suspended)
            return BoundaryDecision.Deny("既に貸出停止中です");

        return BoundaryDecision.Allow();
    }

    /// <summary>
    /// 復帰可否を判定
    /// </summary>
    public BoundaryDecision CanReactivate()
    {
        if (Status == MemberStatus.Active)
            return BoundaryDecision.Deny("既にアクティブです");

        return BoundaryDecision.Allow();
    }

    // ================================================================
    // 状態変更メソッド
    // ================================================================

    /// <summary>
    /// 利用者情報を更新
    /// </summary>
    public void Update(string name, string? email, string? phone)
    {
        Name = name;
        Email = email;
        Phone = phone;
    }

    /// <summary>
    /// 貸出停止にする
    /// </summary>
    public void Suspend()
    {
        var decision = CanSuspend();
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Reason);

        Status = MemberStatus.Suspended;
    }

    /// <summary>
    /// アクティブに戻す
    /// </summary>
    public void Reactivate()
    {
        var decision = CanReactivate();
        if (!decision.IsAllowed)
            throw new InvalidOperationException(decision.Reason);

        Status = MemberStatus.Active;
    }
}
