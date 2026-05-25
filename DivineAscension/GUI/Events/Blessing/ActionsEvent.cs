namespace DivineAscension.GUI.Events.Blessing;

public abstract record ActionsEvent
{
    /// <summary>
    ///     Fired when the Unlock button is clicked and the blessing can be unlocked.
    ///     Opens the confirmation dialog rather than dispatching directly (#453) — the
    ///     favor/prestige spend is only committed once the player confirms.
    /// </summary>
    public sealed record UnlockClicked : ActionsEvent;

    /// <summary>
    ///     Fired when the player confirms the unlock in the confirmation dialog (#453).
    ///     This is the event that actually dispatches the unlock request to the server.
    /// </summary>
    public sealed record UnlockConfirmed : ActionsEvent;

    /// <summary>
    ///     Fired when the player dismisses the unlock confirmation dialog (#453).
    ///     Clears the pending unlock with no side effects.
    /// </summary>
    public sealed record UnlockCanceled : ActionsEvent;

    /// <summary>
    ///     Fired when the player attempts to unlock a locked/disabled blessing
    ///     (e.g., missing rank requirements or unsatisfied prerequisites)
    /// </summary>
    public sealed record UnlockBlockedClicked : ActionsEvent;
}