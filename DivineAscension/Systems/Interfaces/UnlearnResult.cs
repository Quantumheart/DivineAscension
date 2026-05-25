namespace DivineAscension.Systems.Interfaces;

/// <summary>
///     Why an unlearn attempt was rejected. <see cref="None"/> is used on success.
/// </summary>
public enum UnlearnFailureReason
{
    None,
    BlessingNotFound,
    NotOwned,
    NotInReligion,
    NotPlayerBlessing,
    OnCooldown,
    InProgress,
    NotConfigured
}

/// <summary>
///     Outcome of <see cref="IPlayerProgressionService.UnlearnBlessing"/>. Carries enough for the
///     network handler to build a localized response without the service touching localization.
/// </summary>
public readonly record struct UnlearnResult(
    bool Success,
    UnlearnFailureReason Reason,
    int RefundedFavor,
    double RemainingCooldownSeconds)
{
    public static UnlearnResult Ok(int refundedFavor) =>
        new(true, UnlearnFailureReason.None, refundedFavor, 0);

    public static UnlearnResult Fail(UnlearnFailureReason reason, double remainingCooldownSeconds = 0) =>
        new(false, reason, 0, remainingCooldownSeconds);
}
