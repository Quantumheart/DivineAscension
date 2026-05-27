namespace DivineAscension.GUI.Events.Civilization;

/// <summary>
///     UI intents emitted by the civilization Chronicle chapter (#369). The chapter
///     is read-only, so scrolling is the only interaction.
/// </summary>
public abstract record CivilizationChronicleEvent
{
    public record ScrollChanged(float NewScrollY) : CivilizationChronicleEvent;
}
