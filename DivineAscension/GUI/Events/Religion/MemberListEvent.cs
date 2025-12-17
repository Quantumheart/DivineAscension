namespace PantheonWars.GUI.Events.Religion;

/// <summary>
/// Events emitted by the members list renderer
/// </summary>
public abstract record MemberListEvent
{
    public record KickClicked(string PlayerUID) : MemberListEvent;

    public record BanClicked(string PlayerUID) : MemberListEvent;

    public record ScrollChanged(float NewScrollY) : MemberListEvent;
}