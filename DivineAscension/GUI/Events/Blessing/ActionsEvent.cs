namespace DivineAscension.GUI.Events.Blessing;

public abstract record ActionsEvent
{
    /// <summary>
    ///     Fired when the Unlock button is clicked and the blessing can be unlocked
    /// </summary>
    public sealed record UnlockClicked : ActionsEvent;

    /// <summary>
    ///     Fired when the player attempts to unlock a locked/disabled blessing
    ///     (e.g., missing rank requirements or unsatisfied prerequisites)
    /// </summary>
    public sealed record UnlockBlockedClicked : ActionsEvent;
}