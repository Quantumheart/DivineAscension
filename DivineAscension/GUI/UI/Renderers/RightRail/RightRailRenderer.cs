using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.Extensions;
using DivineAscension.GUI.UI.Layout;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.RightRail;

/// <summary>
///     Vertical layout in a 340px-wide column: religion block, civilization block,
///     notification feed. Anchored to a passed <see cref="UiRect" />. Phase 3b
///     wires this into <c>MainLayoutCoordinator</c>; in Phase 3a the file is
///     intentionally not called from production code.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class RightRailRenderer
{
    private const float Padding = 12f;
    private const float BlockSpacing = 10f;
    private const float IconSize = 40f;
    private const float ProgressBarHeight = 12f;
    private const float ProgressBarSpacing = 18f;

    public static void Draw(UiRect rect, RightRailViewModel vm)
    {
        if (rect.W <= 0f || rect.H <= 0f) return;

        var drawList = ImGui.GetWindowDrawList();

        // Outer panel background + border.
        var bg = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        var border = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f);
        drawList.AddRectFilled(new Vector2(rect.X, rect.Y),
            new Vector2(rect.Right, rect.Bottom), bg, 4f);
        drawList.AddRect(new Vector2(rect.X, rect.Y),
            new Vector2(rect.Right, rect.Bottom), border, 4f, ImDrawFlags.None, 2f);

        var inner = rect.Inset(Padding);
        var cursorY = inner.Y;

        cursorY = DrawReligionBlock(drawList, inner, cursorY, vm);
        cursorY = DrawCivilizationBlock(drawList, inner, cursorY + BlockSpacing, vm);
        DrawNotificationFeed(rect, inner, cursorY + BlockSpacing, vm);
    }

    private static float DrawReligionBlock(ImDrawListPtr drawList, UiRect inner, float y,
        RightRailViewModel vm)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);

        if (!vm.HasReligion)
        {
            var msg = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_NO_RELIGION);
            drawList.AddText(ImGui.GetFont(), Body, new Vector2(inner.X, y),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), msg);
            return y + 24f;
        }

        var iconPos = new Vector2(inner.X, y);
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

        var textX = inner.X + IconSize + 8f;
        var religionName = vm.CurrentReligionName
            ?? LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNKNOWN_RELIGION);
        var deityName = !string.IsNullOrEmpty(vm.CurrentDeityName)
            ? vm.CurrentDeityName!
            : vm.CurrentDeity.ToLocalizedString();

        drawList.AddText(ImGui.GetFont(), SectionHeader, new Vector2(textX, y),
            labelColor, religionName);
        drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(textX, y + 22f),
            textColor, deityName);

        var afterIconY = y + IconSize + 6f;

        // Favor progress
        var favor = vm.PlayerFavorProgress;
        var favorLabel = favor.IsMaxRank
            ? $"{RankRequirements.GetFavorRankName(favor.CurrentRank)} (MAX)"
            : $"{RankRequirements.GetFavorRankName(favor.CurrentRank)} ({favor.CurrentFavor}/{favor.RequiredFavor})";
        ProgressBarRenderer.DrawProgressBar(drawList,
            inner.X, afterIconY, inner.W, ProgressBarHeight,
            favor.ProgressPercentage, ColorPalette.Gold, ColorPalette.DarkBrown,
            favorLabel, favor.ProgressPercentage > 0.8f);

        afterIconY += ProgressBarSpacing;

        // Prestige progress
        var prestige = vm.ReligionPrestigeProgress;
        var prestigeLabel = prestige.IsMaxRank
            ? $"{RankRequirements.GetPrestigeRankName(prestige.CurrentRank)} (MAX)"
            : $"{RankRequirements.GetPrestigeRankName(prestige.CurrentRank)} ({prestige.CurrentPrestige}/{prestige.RequiredPrestige})";
        ProgressBarRenderer.DrawProgressBar(drawList,
            inner.X, afterIconY, inner.W, ProgressBarHeight,
            prestige.ProgressPercentage, new Vector4(0.48f, 0.41f, 0.93f, 1f),
            ColorPalette.DarkBrown, prestigeLabel, prestige.ProgressPercentage > 0.8f);

        return afterIconY + ProgressBarHeight + 4f;
    }

    private static float DrawCivilizationBlock(ImDrawListPtr drawList, UiRect inner, float y,
        RightRailViewModel vm)
    {
        // Separator
        var sepColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.3f);
        drawList.AddLine(new Vector2(inner.X, y - BlockSpacing / 2f),
            new Vector2(inner.Right, y - BlockSpacing / 2f), sepColor, 1f);

        if (!vm.HasCivilization && string.IsNullOrEmpty(vm.CurrentCivilizationName))
        {
            var msg = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNKNOWN_CIVILIZATION);
            drawList.AddText(ImGui.GetFont(), Body, new Vector2(inner.X, y),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), msg);
            return y + 24f;
        }

        var iconPos = new Vector2(inner.X, y);
        var civTexture = CivilizationIconLoader.GetIconTextureId(vm.CivilizationIcon ?? "default");
        drawList.AddImage(civTexture, iconPos,
            new Vector2(iconPos.X + IconSize, iconPos.Y + IconSize),
            Vector2.Zero, Vector2.One,
            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
        drawList.AddRect(iconPos,
            new Vector2(iconPos.X + IconSize, iconPos.Y + IconSize),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.8f, 0.8f, 1f)),
            4f, ImDrawFlags.None, 2f);

        var textX = inner.X + IconSize + 8f;
        var civName = vm.CurrentCivilizationName
            ?? LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNKNOWN_CIVILIZATION);
        var rankName = RankRequirements.GetCivilizationRankName(vm.CivilizationRank);

        drawList.AddText(ImGui.GetFont(), SectionHeader, new Vector2(textX, y),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.8f, 1f, 1f)), civName);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, y + 22f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.8f), $"[{rankName}]");

        var rowY = y + IconSize + 6f;
        var memberCount = vm.CivilizationMemberReligions.Count;
        var memberText = LocalizationService.Instance.Get(
            LocalizationKeys.UI_BLESSING_RELIGIONS_COUNT, memberCount);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(inner.X, rowY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), memberText);
        rowY += 18f;

        // Member-religion deity icons.
        if (memberCount > 0)
        {
            var deityX = inner.X;
            const float deityIconSize = 18f;
            const float deityIconSpacing = 4f;
            foreach (var member in vm.CivilizationMemberReligions)
            {
                if (!Enum.TryParse<DeityDomain>(member.Domain, out var deity)) continue;
                var tex = DeityIconLoader.GetDeityTextureId(deity);
                drawList.AddImage(tex,
                    new Vector2(deityX, rowY),
                    new Vector2(deityX + deityIconSize, rowY + deityIconSize),
                    Vector2.Zero, Vector2.One,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
                deityX += deityIconSize + deityIconSpacing;
                if (deityX + deityIconSize > inner.Right) break;
            }
            rowY += deityIconSize + 4f;
        }

        if (vm.IsCivilizationFounder)
        {
            drawList.AddText(ImGui.GetFont(), Secondary, new Vector2(inner.X, rowY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), "*** Founder ***");
            rowY += 18f;
        }

        return rowY;
    }

    private static void DrawNotificationFeed(UiRect outer, UiRect inner, float y,
        RightRailViewModel vm)
    {
        var available = outer.Bottom - y - Padding;
        if (available <= 0f) return;

        ImGui.SetCursorScreenPos(new Vector2(inner.X, y));
        ImGui.BeginChild("##da-rightrail-feed",
            new Vector2(inner.W, available), false,
            ImGuiWindowFlags.None);

        var count = 0;
        for (var i = vm.Notifications.Count - 1; i >= 0; i--)
        {
            var entry = vm.Notifications[i];
            if (vm.ShowUnreadOnly && entry.Read) continue;

            DrawNotificationRow(entry);
            count++;
        }

        if (count == 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPalette.Grey);
            ImGui.TextWrapped("(no notifications)");
            ImGui.PopStyleColor();
        }

        ImGui.EndChild();
    }

    private static void DrawNotificationRow(GUI.State.NotificationHistoryEntry entry)
    {
        var color = entry.Read ? ColorPalette.Grey : ColorPalette.White;
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextWrapped($"{entry.Timestamp:HH:mm}  {entry.Title}");
        ImGui.PopStyleColor();
        if (!string.IsNullOrEmpty(entry.Body))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPalette.Grey);
            ImGui.TextWrapped($"  {entry.Body}");
            ImGui.PopStyleColor();
        }
        ImGui.Spacing();
    }
}
