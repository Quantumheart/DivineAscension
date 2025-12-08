namespace PantheonWars.GUI.Events;

/// <summary>
/// Events emitted by the members list renderer
/// </summary>
public abstract record ReligionMemberListEvent
{
    public record KickClicked(string PlayerUID) : ReligionMemberListEvent;
    public record BanClicked(string PlayerUID) : ReligionMemberListEvent;
    public record ScrollChanged(float NewScrollY) : ReligionMemberListEvent;
}
