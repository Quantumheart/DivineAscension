namespace DivineAscension.Models;

/// <summary>
///     Tracks progress toward a specific milestone
/// </summary>
public class MilestoneProgress
{
    /// <summary>
    ///     Milestone ID this progress tracks
    /// </summary>
    public string MilestoneId { get; }

    /// <summary>
    ///     Display name of the milestone
    /// </summary>
    public string MilestoneName { get; }

    /// <summary>
    ///     Current progress value
    /// </summary>
    public int CurrentValue { get; }

    /// <summary>
    ///     Target value required to complete the milestone
    /// </summary>
    public int TargetValue { get; }

    /// <summary>
    ///     Whether this milestone has been completed
    /// </summary>
    public bool IsCompleted { get; }

    /// <summary>
    ///     Progress percentage (0.0 to 1.0)
    /// </summary>
    public float ProgressPercent => TargetValue > 0 ? (float)CurrentValue / TargetValue : 0f;

    public MilestoneProgress(
        string milestoneId,
        string milestoneName,
        int currentValue,
        int targetValue,
        bool isCompleted)
    {
        MilestoneId = milestoneId;
        MilestoneName = milestoneName;
        CurrentValue = currentValue;
        TargetValue = targetValue;
        IsCompleted = isCompleted;
    }
}
