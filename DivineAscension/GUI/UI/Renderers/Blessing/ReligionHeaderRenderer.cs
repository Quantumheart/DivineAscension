using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.Extensions;
using DivineAscension.GUI.Models.Religion.Header;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Renders religion + civilization status as two stacked vertical blocks
///     sized to a narrow column. Used by the right-rail in the Phase 3b layout.
///     Returns the total pixel height consumed so the caller can place content
///     below.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionHeaderRenderer
{
    private const float Padding = 12f;
    private const float IconSize = 40f;
    private const float ProgressBarHeight = 12f;
    private const float RowSpacing = 6f;
    private const float BlockSpacing = 10f;

    /// <summary>
    ///     Draw the two-block status into the rect at (vm.X, vm.Y, vm.Width).
    ///     Panel chrome (background + border) is the caller's responsibility —
    ///     this renderer paints content only. Returns total content height.
    /// </summary>
    public static float Draw(ReligionHeaderViewModel vm)
    {
        var drawList = ImGui.GetWindowDrawList();

        var cursorY = vm.Y + Padding;
        var innerX = vm.X + Padding;
        var innerW = vm.Width - Padding * 2f;

        cursorY = DrawReligionBlock(drawList, innerX, cursorY, innerW, vm);
        cursorY += BlockSpacing;
        cursorY = DrawCivilizationBlock(drawList, innerX, cursorY, innerW, vm);

        return cursorY + Padding - vm.Y;
    }

    private static float DrawReligionBlock(ImDrawListPtr drawList, float x, float y, float width,
        ReligionHeaderViewModel vm)
    {
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        var subColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);

        if (!vm.HasReligion)
        {
            var msg = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_NO_RELIGION);
            drawList.AddText(ImGui.GetFont(), Body, new Vector2(x, y), subColor, msg);
            return y + 22f;
        }

        // Deity icon + name row.
        var iconPos = new Vector2(x, y);
        var deityTexture = DeityIconLoader.GetDeityTextureId(vm.CurrentDeity);
        if (deityTexture != IntPtr.Zero)
        {
            drawList.AddImage(deityTexture, iconPos,
                new Vector2(iconPos.X + IconSize, iconPos.Y + IconSize),
                Vector2.Zero, Vector2.One,
                ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
            drawList.AddRect(iconPos,
                new Vector2(iconPos.X + IconSize, iconPos.Y + IconSize),
                ImGui.ColorConvertFloat4ToU32(DomainHelper.GetDeityColor(vm.CurrentDeity) * 0.8f),
                4f, ImDrawFlags.None, 2f);
        }
        else
        {
            var fallback = ImGui.ColorConvertFloat4ToU32(DomainHelper.GetDeityColor(vm.CurrentDeity));
            drawList.AddCircleFilled(
                new Vector2(iconPos.X + IconSize / 2f, iconPos.Y + IconSize / 2f),
                IconSize / 2f, fallback, 16);
        }

        var textX = x + IconSize + 8f;
        var religionName = vm.CurrentReligionName
            ?? LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNKNOWN_RELIGION);
        var deityName = !string.IsNullOrEmpty(vm.CurrentDeityName)
            ? vm.CurrentDeityName!
            : vm.CurrentDeity.ToLocalizedString();

        drawList.AddText(ImGui.GetFont(), SectionHeader, new Vector2(textX, y),
            labelColor, religionName);
        drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(textX, y + 22f),
            textColor, deityName);

        var cursorY = y + IconSize + RowSpacing;

        // Member count + role on one line.
        var memberInfo = vm.ReligionMemberCount > 0
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_MEMBER_COUNT,
                vm.ReligionMemberCount, vm.ReligionMemberCount == 1 ? "" : "s")
            : LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_NO_MEMBERS);
        var roleInfo = !string.IsNullOrEmpty(vm.PlayerRoleInReligion)
            ? $" | {vm.PlayerRoleInReligion}"
            : "";
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(x, cursorY), subColor,
            $"{memberInfo}{roleInfo}");
        cursorY += 18f;

        // Favor progress
        var favor = vm.PlayerFavorProgress;
        var favorLabel = favor.IsMaxRank
            ? $"{RankRequirements.GetFavorRankName(favor.CurrentRank)} (MAX)"
            : $"{RankRequirements.GetFavorRankName(favor.CurrentRank)} ({favor.CurrentFavor}/{favor.RequiredFavor})";
        ProgressBarRenderer.DrawProgressBar(drawList, x, cursorY, width, ProgressBarHeight,
            favor.ProgressPercentage, ColorPalette.Gold, ColorPalette.DarkBrown,
            favorLabel, favor.ProgressPercentage > 0.8f);
        cursorY += ProgressBarHeight + 6f;

        // Prestige progress — lapis bar so prestige reads as the "second ink"
        // alongside the gold favor bar (gold leaf + lapis blue = the classic
        // two-accent system in real illuminated manuscripts).
        var prestige = vm.ReligionPrestigeProgress;
        var prestigeLabel = prestige.IsMaxRank
            ? $"{RankRequirements.GetPrestigeRankName(prestige.CurrentRank)} (MAX)"
            : $"{RankRequirements.GetPrestigeRankName(prestige.CurrentRank)} ({prestige.CurrentPrestige}/{prestige.RequiredPrestige})";
        ProgressBarRenderer.DrawProgressBar(drawList, x, cursorY, width, ProgressBarHeight,
            prestige.ProgressPercentage, ColorPalette.Lapis,
            ColorPalette.DarkBrown, prestigeLabel, prestige.ProgressPercentage > 0.8f);
        cursorY += ProgressBarHeight + 2f;

        return cursorY;
    }

    private static float DrawCivilizationBlock(ImDrawListPtr drawList, float x, float y, float width,
        ReligionHeaderViewModel vm)
    {
        var sepColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.3f);
        drawList.AddLine(new Vector2(x, y - BlockSpacing / 2f),
            new Vector2(x + width, y - BlockSpacing / 2f), sepColor, 1f);

        var hasAny = vm.HasCivilization
                     || !string.IsNullOrEmpty(vm.CurrentCivilizationName)
                     || vm.CivilizationMemberReligions?.Count > 0;
        if (!hasAny)
        {
            var msg = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNKNOWN_CIVILIZATION);
            drawList.AddText(ImGui.GetFont(), Body, new Vector2(x, y),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), msg);
            return y + 22f;
        }

        var iconPos = new Vector2(x, y);
        var civTexture = CivilizationIconLoader.GetIconTextureId(vm.CivilizationIcon ?? "default");
        drawList.AddImage(civTexture, iconPos,
            new Vector2(iconPos.X + IconSize, iconPos.Y + IconSize),
            Vector2.Zero, Vector2.One,
            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
        drawList.AddRect(iconPos,
            new Vector2(iconPos.X + IconSize, iconPos.Y + IconSize),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.8f, 0.8f, 1f)),
            4f, ImDrawFlags.None, 2f);

        var textX = x + IconSize + 8f;
        var civName = vm.CurrentCivilizationName
            ?? LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNKNOWN_CIVILIZATION);
        var rankName = RankRequirements.GetCivilizationRankName(vm.CivilizationRank);
        // Civilization name in lapis — the second accent ink. Pairs with the
        // gold-leaf religion name above to give the rail a clear two-ink hierarchy.
        drawList.AddText(ImGui.GetFont(), SectionHeader, new Vector2(textX, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Lapis), civName);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, y + 22f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.8f), $"[{rankName}]");

        var cursorY = y + IconSize + RowSpacing;

        var memberCount = vm.CivilizationMemberReligions?.Count ?? 0;
        var memberText = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_RELIGIONS_COUNT, memberCount);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(x, cursorY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), memberText);
        cursorY += 18f;

        if (memberCount > 0 && vm.CivilizationMemberReligions != null)
        {
            const float deityIconSize = 18f;
            const float deityIconSpacing = 4f;
            var deityX = x;
            foreach (var member in vm.CivilizationMemberReligions)
            {
                if (!Enum.TryParse<DeityDomain>(member.Domain, out var deity)) continue;
                var tex = DeityIconLoader.GetDeityTextureId(deity);
                drawList.AddImage(tex,
                    new Vector2(deityX, cursorY),
                    new Vector2(deityX + deityIconSize, cursorY + deityIconSize),
                    Vector2.Zero, Vector2.One,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
                deityX += deityIconSize + deityIconSpacing;
                if (deityX + deityIconSize > x + width) break;
            }
            cursorY += deityIconSize + 4f;
        }

        if (vm.IsCivilizationFounder)
        {
            // Founder rubric — vermilion ink, the historical "rubrum" used to
            // mark titles and people of rank. TODO: localize this label.
            drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(x, cursorY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Vermilion), "Founder");
            cursorY += 18f;
        }

        return cursorY;
    }
}
