namespace DivineAscension.GUI.Events;

/// <summary>
///     Marker for pane events that drive a modal overlay's own confirm/cancel and must
///     stay live while that modal is open. Everything <em>else</em> a pane emits is
///     "background" interaction (selection, scroll, secondary buttons) that fell through
///     behind the dim backdrop in immediate mode — see
///     <see cref="DivineAscension.GUI.UI.Utilities.ModalInputGuard" />.
///
///     A pane's event processor passes its frame's events through
///     <see cref="DivineAscension.GUI.UI.Utilities.ModalInputGuard.FilterBackground{T}" />,
///     which drops un-marked events while a modal is up. Confirm/cancel records carry this
///     marker so they survive the filter; the modal's buttons therefore keep working even
///     though the pane behind it is inert. New confirm modals get the gate for free by
///     marking their confirm/cancel events.
/// </summary>
public interface IModalControlEvent;
