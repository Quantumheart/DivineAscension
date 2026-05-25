namespace DivineAscension.GUI.Events.Blessing;

public abstract record ActionsEvent
{
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
    ///     Fired when the player confirms unlearning a blessing. Dispatches the unlearn request.
    /// </summary>
    public sealed record UnlearnConfirmed : ActionsEvent;

    /// <summary>
    ///     Fired when the player dismisses the unlearn confirmation dialog with no side effects.
    /// </summary>
    public sealed record UnlearnCanceled : ActionsEvent;
}
