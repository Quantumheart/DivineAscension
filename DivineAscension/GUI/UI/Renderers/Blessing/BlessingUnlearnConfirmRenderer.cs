using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.Models;
using DivineAscension.Services;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Modal confirmation for unlearning a personal blessing (#459). Mirrors the unlock
///     confirm (<see cref="BlessingUnlockConfirmRenderer" />): unlearning refunds half the
///     favor paid and forfeits the rest, so the action is gated behind an explicit confirm.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingUnlearnConfirmRenderer
{
    public static void Draw(BlessingNodeState pending, List<ActionsEvent> events)
    {
        var blessing = pending.Blessing;

        // The favor the player actually paid (patron multiplier applied client-side mirrors
        // the server's AdjustedCost). The server refunds half of this.
        var paidCost = (int)(blessing.Cost * pending.NonPatronCostMultiplier);

        var title = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_TITLE);
        var message = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_MESSAGE, blessing.Name, paidCost);
        var confirmLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNLEARN_BUTTON);

        ConfirmOverlay.Draw(title, message, out var confirmed, out var canceled, confirmLabel);

        if (confirmed) events.Add(new ActionsEvent.UnlearnConfirmed());
        if (canceled) events.Add(new ActionsEvent.UnlearnCanceled());
    }
}
