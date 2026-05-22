using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using DivineAscension.Constants;
using DivineAscension.GUI.Models.Civilization.Info;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization.Info;

/// <summary>
/// Prose intro line + dotted-leader stat block for the "This Realm" chapter.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationInfoHeaderRenderer
{
    private const float StatRowHeight = 22f;
    private const float StatBlockBottomSpacing = 8f;
    private const float ProseBottomSpacing = 12f;

    public static float Draw(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width)
    {
        var currentY = DrawProseIntro(vm, drawList, x, y, width);
        return DrawStatBlock(vm, drawList, x, currentY, width);
    }

    private static float DrawProseIntro(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var founder = string.IsNullOrWhiteSpace(vm.FounderName) ? string.Empty : vm.FounderName;
        var month = vm.CreatedDate == DateTime.MinValue
            ? string.Empty
            : vm.CreatedDate.ToString("MMMM", CultureInfo.InvariantCulture);

        var founded = string.IsNullOrEmpty(month) || string.IsNullOrEmpty(founder)
            ? string.Empty
            : LocalizationService.Instance.Get(
                LocalizationKeys.UI_CIVILIZATION_INFO_INTRO_FOUNDED, month, founder);

        var bannerKey = vm.MemberCount == 1
            ? LocalizationKeys.UI_CIVILIZATION_INFO_INTRO_BANNER_ONE
            : LocalizationKeys.UI_CIVILIZATION_INFO_INTRO_BANNER;
        var banner = vm.MemberCount == 1
            ? LocalizationService.Instance.Get(bannerKey)
            : LocalizationService.Instance.Get(bannerKey, vm.MemberCount);

        var prose = string.IsNullOrEmpty(founded) ? banner : $"{founded} {banner}";

        TextRenderer.DrawInfoText(drawList, prose, x, y, width, Body, ColorPalette.White);
        var height = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        return y + (height > 0 ? height : Body + LinePadding) + ProseBottomSpacing;
    }

    private static float DrawStatBlock(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var currentY = y;

        var foundedDate = vm.CreatedDate == DateTime.MinValue
            ? "—"
            : vm.CreatedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_FOUNDED),
            foundedDate,
            x, currentY, width);
        currentY += StatRowHeight;

        var founderValue = string.IsNullOrWhiteSpace(vm.FounderName) ? "—" : vm.FounderName;
        if (!string.IsNullOrWhiteSpace(vm.FounderEpithet))
            founderValue = $"{founderValue}, {vm.FounderEpithet}";

        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_FOUNDER),
            founderValue,
            x, currentY, width,
            valueColor: ColorPalette.Vermilion);
        currentY += StatRowHeight;

        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_FOUNDING_ORDER),
            string.IsNullOrWhiteSpace(vm.FounderReligionName) ? "—" : vm.FounderReligionName,
            x, currentY, width);
        currentY += StatRowHeight;

        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_ETHOS),
            LocalizationService.Instance.Get(EthosLocKey((CivilizationEthos)vm.Ethos)),
            x, currentY, width);
        currentY += StatRowHeight;

        return currentY + StatBlockBottomSpacing;
    }

    private static string EthosLocKey(CivilizationEthos ethos) => ethos switch
    {
        CivilizationEthos.Mercantile => LocalizationKeys.CIVILIZATION_ETHOS_MERCANTILE,
        CivilizationEthos.Martial => LocalizationKeys.CIVILIZATION_ETHOS_MARTIAL,
        CivilizationEthos.Mystic => LocalizationKeys.CIVILIZATION_ETHOS_MYSTIC,
        CivilizationEthos.Ascetic => LocalizationKeys.CIVILIZATION_ETHOS_ASCETIC,
        _ => LocalizationKeys.CIVILIZATION_ETHOS_SOVEREIGN
    };
}
