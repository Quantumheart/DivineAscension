using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Updates the player's prayer cooldown after a successful prayer.
/// Only executes if the prayer was successful and ShouldUpdateCooldown is true.
/// </summary>
public class CooldownUpdateStep : IPrayerStep
{
    private const int PRAYER_COOLDOWN_MS = 3600000; // 1 hour

    private readonly IPlayerProgressionDataManager _progressionDataManager;

    public CooldownUpdateStep(IPlayerProgressionDataManager progressionDataManager)
    {
        _progressionDataManager = progressionDataManager;
    }

    public string Name => "CooldownUpdate";

    public void Execute(PrayerContext context)
    {
        if (!context.Success || !context.ShouldUpdateCooldown)
            return;

        _progressionDataManager.SetPrayerCooldownExpiry(
            context.PlayerUID,
            context.CurrentTime + PRAYER_COOLDOWN_MS);
    }
}