namespace DivineAscension.GUI.Events.Religion;

/// <summary>
///     UI intents emitted by the Sacred Calendar chapter (#375). Read-only;
///     scroll is the only interaction.
/// </summary>
public abstract record SacredCalendarEvent
{
    public record ScrollChanged(float NewScrollY) : SacredCalendarEvent;

    /// <summary>Founder clicked the "+ Add feast day" button.</summary>
    public record AddDialogOpened : SacredCalendarEvent;

    /// <summary>Founder canceled the add dialog.</summary>
    public record AddDialogCancel : SacredCalendarEvent;

    public record AddNameChanged(string NewName) : SacredCalendarEvent;
    public record AddMonthChanged(int NewMonth) : SacredCalendarEvent;
    public record AddDayChanged(int NewDay) : SacredCalendarEvent;

    /// <summary>Founder confirmed; send Add to server.</summary>
    public record AddSubmitted(string Name, int Month, int Day) : SacredCalendarEvent;

    /// <summary>Founder clicked trash on a custom feast row.</summary>
    public record RemoveRequested(System.Guid FeastId, string Name) : SacredCalendarEvent;

    /// <summary>Founder confirmed removal; send Remove to server.</summary>
    public record RemoveConfirmed(System.Guid FeastId) : SacredCalendarEvent;

    public record RemoveCancel : SacredCalendarEvent;

    /// <summary>User dismissed the error banner.</summary>
    public record DismissError : SacredCalendarEvent;
}
