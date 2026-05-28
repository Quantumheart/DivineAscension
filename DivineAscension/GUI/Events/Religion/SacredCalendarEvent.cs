namespace DivineAscension.GUI.Events.Religion;

/// <summary>
///     UI intents emitted by the Sacred Calendar chapter (#375). Read-only;
///     scroll is the only interaction.
/// </summary>
public abstract record SacredCalendarEvent
{
    public record ScrollChanged(float NewScrollY) : SacredCalendarEvent;
}
