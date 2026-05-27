namespace DivineAscension.GUI.Events.Religion;

/// <summary>
///     UI intents emitted by the religion Chronicle chapter (#373). The chapter is
///     read-only, so scrolling is the only interaction.
/// </summary>
public abstract record ChronicleEvent
{
    public record ScrollChanged(float NewScrollY) : ChronicleEvent;
}
