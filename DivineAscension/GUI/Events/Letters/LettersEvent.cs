namespace DivineAscension.GUI.Events.Letters;

/// <summary>
///     Letter-scoped events emitted by the shared
///     <see cref="DivineAscension.GUI.UI.Renderers.Components.LettersRenderer" />.
///     Adapters (religion / civilization invites) translate these into their
///     own area-specific event records before handing them to the state
///     manager.
/// </summary>
public abstract record LettersEvent
{
    public sealed record AcceptClicked(string Id) : LettersEvent;

    public sealed record RefuseClicked(string Id) : LettersEvent;

    public sealed record ScrollChanged(float NewScrollY) : LettersEvent;
}
