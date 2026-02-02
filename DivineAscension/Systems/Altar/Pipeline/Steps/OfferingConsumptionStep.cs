namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Consumes the offering item from the player's hand if needed.
/// Only executes if the prayer was successful and ShouldConsumeOffering is true.
/// </summary>
public class OfferingConsumptionStep : IPrayerStep
{
    public string Name => "OfferingConsumption";

    public void Execute(PrayerContext context)
    {
        if (!context.Success || !context.ShouldConsumeOffering)
            return;

        var slot = context.Player.Entity.RightHandItemSlot;
        if (slot != null)
        {
            slot.TakeOut(1);
            slot.MarkDirty();
        }
    }
}