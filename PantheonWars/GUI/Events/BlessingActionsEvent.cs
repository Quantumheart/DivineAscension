namespace PantheonWars.GUI.Events;

public abstract record BlessingActionsEvent
{
    /// <summary>
    ///     Fired when the Close button is clicked
    /// </summary>
    public sealed record CloseClicked : BlessingActionsEvent;

    /// <summary>
    ///     Fired when the Unlock button is clicked and the blessing can be unlocked
    /// </summary>
    public sealed record UnlockClicked : BlessingActionsEvent;

    /// <summary>
    ///     Fired when the player attempts to unlock a locked/disabled blessing
    ///     (e.g., missing rank requirements or unsatisfied prerequisites)
    /// </summary>
    public sealed record UnlockBlockedClicked : BlessingActionsEvent;
}