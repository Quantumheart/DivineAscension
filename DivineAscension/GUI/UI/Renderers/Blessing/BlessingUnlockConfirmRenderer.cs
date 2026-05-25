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
///     Modal confirmation for unlocking a blessing (#453). Unlocking permanently spends
///     favor (personal) or prestige (religion) and, for religion-kind blessings, binds a
///     vow on behalf of the whole Order — so the spend is gated behind an explicit confirm.
///     Personal unlocks use the "Inscribe" voice; communal vows use the "Swear" voice.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingUnlockConfirmRenderer
{
    public static void Draw(BlessingNodeState pending, List<ActionsEvent> events)
    {
        var blessing = pending.Blessing;
        var isReligionKind = blessing.Kind == BlessingKind.Religion;

        var title = LocalizationService.Instance.Get(isReligionKind
            ? LocalizationKeys.UI_BLESSING_CONFIRM_SWEAR_TITLE
            : LocalizationKeys.UI_BLESSING_CONFIRM_INSCRIBE_TITLE);

        var message = LocalizationService.Instance.Get(
            isReligionKind
                ? LocalizationKeys.UI_BLESSING_CONFIRM_SWEAR_MESSAGE
                : LocalizationKeys.UI_BLESSING_CONFIRM_INSCRIBE_MESSAGE,
            blessing.Name,
            blessing.Cost);

        var confirmLabel = LocalizationService.Instance.Get(isReligionKind
            ? LocalizationKeys.UI_BLESSING_SWEAR_BUTTON
            : LocalizationKeys.UI_BLESSING_INSCRIBE_BUTTON);

        ConfirmOverlay.Draw(title, message, out var confirmed, out var canceled, confirmLabel);

        if (confirmed) events.Add(new ActionsEvent.UnlockConfirmed());
        if (canceled) events.Add(new ActionsEvent.UnlockCanceled());
    }
}
