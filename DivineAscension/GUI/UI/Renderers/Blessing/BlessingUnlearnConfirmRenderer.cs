using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
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
        var isReligion = blessing.Kind == BlessingKind.Religion;

        // Religion vows reclaim prestige and use founder-voiced strings (#484); personal blessings
        // reclaim favor. Both share the cascade list header and Strike confirm label.
        var title = isReligion
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_CONFIRM_RELIGION_STRIKE_TITLE)
            : LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_TITLE);

        // cascadeNames includes the target first; count > 1 means children cascade too.
        var isCascade = cascadeNames is { Count: > 1 };

        string message;
        if (isCascade)
        {
            var dependents = cascadeNames!.Count - 1;
            var cascadeKey = isReligion
                ? LocalizationKeys.UI_BLESSING_CONFIRM_RELIGION_STRIKE_CASCADE_MESSAGE
                : LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_CASCADE_MESSAGE;
            message = LocalizationService.Instance.Get(cascadeKey, blessing.Name, dependents, refundTotal)
                      + " "
                      + LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_CASCADE_LIST_HEADER)
                      + " "
                      + string.Join(", ", cascadeNames!);
        }
        else
        {
            // The cost actually paid (patron multiplier mirrors the server's AdjustedCost).
            var paidCost = (int)(blessing.Cost * pending.NonPatronCostMultiplier);
            var messageKey = isReligion
                ? LocalizationKeys.UI_BLESSING_CONFIRM_RELIGION_STRIKE_MESSAGE
                : LocalizationKeys.UI_BLESSING_CONFIRM_UNLEARN_MESSAGE;
            message = LocalizationService.Instance.Get(messageKey, blessing.Name, paidCost);
        }

        var confirmLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNLEARN_BUTTON);

        ConfirmOverlay.Draw(title, message, out var confirmed, out var canceled, confirmLabel);

        if (confirmed) events.Add(new ActionsEvent.UnlearnConfirmed());
        if (canceled) events.Add(new ActionsEvent.UnlearnCanceled());
    }
}
