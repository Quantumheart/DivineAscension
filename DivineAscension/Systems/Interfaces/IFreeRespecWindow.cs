using System;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
///     Server-side free-respec window (epic #425, slice 4 — #462). While the window is open,
///     unlearning a personal blessing refunds <b>100%</b> of each blessing's favor cost instead
///     of the normal <c>GameBalanceConfig.UnlearnRefundPercent</c>. Opened/closed only by an
///     admin via <c>/blessings rebalance</c> — there is no per-rank freebie (locked decision 7).
///     Cooldown was omitted from the unlearn flow, so there is nothing to bypass; the window's
///     sole effect is the full refund.
/// </summary>
public interface IFreeRespecWindow
{
    /// <summary>True while the free-respec window is open.</summary>
    bool IsActive { get; }

    /// <summary>Sets the window state. Raises <see cref="Changed"/> when the value flips.</summary>
    void SetActive(bool active);

    /// <summary>Flips the window state and returns the new value. Raises <see cref="Changed"/>.</summary>
    bool Toggle();

    /// <summary>Raised whenever the window opens or closes, so the UI can be refreshed live.</summary>
    event Action? Changed;
}
