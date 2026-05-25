using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.Models;
using DivineAscension.Services;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Modal confirmation for striking a personal blessing (#459) and its prerequisite cascade
///     (#460). For a lone blessing it mirrors the unlock confirm. When dependent children would
///     also be struck, it names the full kill list and previews the total favor reclaimed so the
///     player sees the consequence before committing.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingUnlearnConfirmRenderer
{
    public static void Draw(
        BlessingNodeState pending,
        IReadOnlyList<string>? cascadeNames,
        int refundTotal,
        List<ActionsEvent> events)
    {
        var blessing = pending.Blessing;
        var title = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_TITLE);

        // cascadeNames includes the target first; count > 1 means children cascade too.
        var isCascade = cascadeNames is { Count: > 1 };

        string message;
        if (isCascade)
        {
            var dependents = cascadeNames!.Count - 1;
            message = LocalizationService.Instance.Get(
                          LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_CASCADE_MESSAGE,
                          blessing.Name, dependents, refundTotal)
                      + " "
                      + LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_CASCADE_LIST_HEADER)
                      + " "
                      + string.Join(", ", cascadeNames!);
        }
        else
        {
            // The favor the player actually paid (patron multiplier mirrors the server's AdjustedCost).
            var paidCost = (int)(blessing.Cost * pending.NonPatronCostMultiplier);
            message = LocalizationService.Instance.Get(
                LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_MESSAGE, blessing.Name, paidCost);
        }

        var confirmLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNLEARN_BUTTON);

        ConfirmOverlay.Draw(title, message, out var confirmed, out var canceled, confirmLabel);

        if (confirmed) events.Add(new ActionsEvent.UnlearnConfirmed());
        if (canceled) events.Add(new ActionsEvent.UnlearnCanceled());
    }
}
