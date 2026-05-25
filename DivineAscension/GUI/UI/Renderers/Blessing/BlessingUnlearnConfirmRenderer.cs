using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.Models;
using DivineAscension.Services;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Modal confirmation for unlearning (respeccing) a personal blessing. Unlearning frees the
///     unlock slot and refunds part of the favor paid, then puts unlearn on cooldown — so the
///     action is gated behind an explicit confirm, mirroring the unlock dialog.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingUnlearnConfirmRenderer
{
    public static void Draw(BlessingNodeState pending, List<ActionsEvent> events)
    {
        var blessing = pending.Blessing;

        var title = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_TITLE);
        var message = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_MESSAGE, blessing.Name);
        var confirmLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNLEARN_BUTTON);

        ConfirmOverlay.Draw(title, message, out var confirmed, out var canceled, confirmLabel);

        if (confirmed) events.Add(new ActionsEvent.UnlearnConfirmed());
        if (canceled) events.Add(new ActionsEvent.UnlearnCanceled());
    }
}
