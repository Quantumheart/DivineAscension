using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.Extensions;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Detail;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Services;
using DivineAscension.Systems;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Renders detailed view of a religion (from browse)
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionDetailRenderer
{
    public static ReligionDetailRendererResult Draw(
        ReligionDetailViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<DetailEvent>();
        var currentY = vm.Y;

        // Loading state
        if (vm.IsLoading)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DETAIL_LOADING), vm.X, currentY + 8f,
                vm.Width);
            return new ReligionDetailRendererResult(events, vm.Height);
        }

        // Back button (top left)
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DETAIL_BACK), vm.X, currentY, 160f, 32f,
                directoryPath: "GUI", iconName: "back"))
            events.Add(new DetailEvent.BackToBrowseClicked());

        // Join button (top right) - only if player can join
        if (vm.CanJoin)
        {
            var joinButtonX = vm.X + vm.Width - 130f - 16f;
            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTION_JOIN), joinButtonX, currentY,
                    130f, 36f,
                    isPrimary: true))
                events.Add(new DetailEvent.JoinClicked(vm.ReligionUID));
        }

        currentY += 44f;

        // Draw background panel per CSS spec (#241B14, 4px border radius)
        var backgroundY = currentY;
        var backgroundHeight = vm.Height - (currentY - vm.Y) - 8f;
        drawList.AddRectFilled(
            new Vector2(vm.X, backgroundY),
            new Vector2(vm.X + vm.Width, backgroundY + backgroundHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.TableBackground), // #241B14
            4f // 4px border radius per CSS
        );

        // Info grid section - Column headers: Name | Deity | Prestige | Public
        DrawInfoGrid(vm, drawList, ref currentY);

        // Description section
        if (!string.IsNullOrEmpty(vm.Description))
        {
            DrawDescriptionSection(vm, drawList, ref currentY);
        }

        // Members section
        DrawMembersSection(vm, drawList, ref currentY, events);

        return new ReligionDetailRendererResult(events, vm.Height);
    }

    private static void DrawInfoGrid(ReligionDetailViewModel vm, ImDrawListPtr drawList, ref float currentY)
    {
        var startY = currentY;
        var font = ImGui.GetFont();

        // Fixed column positions per CSS spec (each column is 270px wide)
        const float columnWidth = 270f;
        const float nameColumnLeft = 291f;
        const float deityColumnLeft = 565f;
        const float prestigeColumnLeft = 839f;
        const float publicColumnLeft = 1113f;

        // Calculate column centers for text alignment
        var nameCenter = vm.X + nameColumnLeft + (columnWidth / 2f);
        var deityCenter = vm.X + deityColumnLeft + (columnWidth / 2f);
        var prestigeCenter = vm.X + prestigeColumnLeft + (columnWidth / 2f);
        var publicCenter = vm.X + publicColumnLeft + (columnWidth / 2f);
        // Column headers - centered using exact text measurements
        var headerColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.239f, 0.180f, 0.125f, 1f)); // #3D2E20

        // Name header (centered in 270px column at 291px)
        var nameHeader = LocalizationService.Instance.Get(LocalizationKeys.UI_TABLE_NAME);
        var nameHeaderWidth = ImGui.CalcTextSize(nameHeader).X;
        drawList.AddText(font, 16f, new Vector2(nameCenter - nameHeaderWidth / 2f, currentY), headerColor, nameHeader);

        // Deity header
        var deityHeader = LocalizationService.Instance.Get(LocalizationKeys.UI_TABLE_DOMAIN);
        var deityHeaderWidth = ImGui.CalcTextSize(deityHeader).X;
        drawList.AddText(font, 16f, new Vector2(deityCenter - deityHeaderWidth / 2f, currentY), headerColor,
            deityHeader);

        // Prestige header
        var prestigeHeader = LocalizationService.Instance.Get(LocalizationKeys.UI_TABLE_PRESTIGE);
        var prestigeHeaderWidth = ImGui.CalcTextSize(prestigeHeader).X;
        drawList.AddText(font, 16f, new Vector2(prestigeCenter - prestigeHeaderWidth / 2f, currentY), headerColor,
            prestigeHeader);

        // Public header
        var publicHeader = LocalizationService.Instance.Get(LocalizationKeys.UI_TABLE_PUBLIC);
        var publicHeaderWidth = ImGui.CalcTextSize(publicHeader).X;
        drawList.AddText(font, 16f, new Vector2(publicCenter - publicHeaderWidth / 2f, currentY), headerColor,
            publicHeader);

        currentY += 32f;

        // Deity icon - fixed position per CSS spec (left: 109px from container start)
        const float iconSize = 85f;
        const float iconLeftOffset = 97; // From CSS spec
        var iconX = vm.X + iconLeftOffset;
        var iconY = currentY;

        if (Enum.TryParse<DeityDomain>(vm.Deity, out var deityType))
        {
            var deityTextureId = DeityIconLoader.GetDeityTextureId(deityType);
            if (deityTextureId != IntPtr.Zero)
            {
                var borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.239f, 0.180f, 0.125f, 1f)); // #3D2E20
                // Draw border (2px)
                drawList.AddRect(
                    new Vector2(iconX - 1f, iconY - 1f),
                    new Vector2(iconX + iconSize + 1f, iconY + iconSize + 1f),
                    borderColor, 4f, ImDrawFlags.None, 2f);

                // Draw icon
                drawList.AddImage(deityTextureId,
                    new Vector2(iconX, iconY),
                    new Vector2(iconX + iconSize, iconY + iconSize),
                    Vector2.Zero, Vector2.One,
                    ImGui.ColorConvertFloat4ToU32(Vector4.One));
            }
        }

        // Religion name - in Name column (centered in 270px column at 291px)
        var nameColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.996f, 0.682f, 0.204f, 1f)); // #FEAE34
        var nameApproxWidth = vm.ReligionName.Length * 6.5f;

        // Name vertically aligned with icon center
        var nameY = iconY + (iconSize - 16f) / 2f;
        drawList.AddText(font, 13f,
            new Vector2(nameCenter - nameApproxWidth / 2f, nameY),
            nameColor, vm.ReligionName);

        // Values centered in their columns (vertically centered with icon)
        var valueColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.573f, 0.502f, 0.416f, 1f)); // #92806A
        var deityValueY = iconY + (iconSize - 16f) / 2f;

        // Deity name with full title (multi-line if needed)
        // Use custom deity name if available, otherwise fall back to domain-based display
        var deityDisplayName = !string.IsNullOrWhiteSpace(vm.DeityName)
            ? vm.DeityName
            : GetDomainDisplayTitle(deityType);
        var deityLines = deityDisplayName.Split('\n');
        var deityStartY = deityValueY - (deityLines.Length > 1 ? 8f : 0f); // Adjust if multi-line

        foreach (var line in deityLines)
        {
            var lineApproxWidth = line.Length * 6.5f;
            drawList.AddText(font, 13f,
                new Vector2(deityCenter - lineApproxWidth / 2f, deityStartY),
                valueColor, line);
            deityStartY += 16f; // Line height
        }

        // Prestige rank with progress (format: "Fledgling Prestige (149/500)")
        var prestigeRankNum = GetPrestigeRankNumber(vm.PrestigeRank);
        var requiredPrestige = RankRequirements.GetRequiredPrestigeForNextRank(prestigeRankNum);
        var localizedRankName = GetLocalizedPrestigeRankName(vm.PrestigeRank);
        var prestigeLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_TABLE_PRESTIGE);
        var prestigeDisplay = requiredPrestige > 0
            ? $"{localizedRankName} {prestigeLabel}\n({vm.Prestige}/{requiredPrestige})"
            : $"{localizedRankName} {prestigeLabel}\n(MAX)";
        var prestigeLines = prestigeDisplay.Split('\n');
        var prestigeStartY = deityValueY - (prestigeLines.Length > 1 ? 8f : 0f); // Adjust if multi-line

        foreach (var line in prestigeLines)
        {
            var lineApproxWidth = line.Length * 6.5f;
            drawList.AddText(font, 13f,
                new Vector2(prestigeCenter - lineApproxWidth / 2f, prestigeStartY),
                valueColor, line);
            prestigeStartY += 16f; // Line height
        }

        // Public/Private (show as Yes/No per spec)
        var publicText = vm.IsPublic
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_PUBLIC)
            : LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_PRIVATE);
        var publicApproxWidth = publicText.Length * 6.5f;
        drawList.AddText(font, 13f,
            new Vector2(publicCenter - publicApproxWidth / 2f, deityValueY),
            valueColor, publicText);

        currentY = iconY + iconSize + 32f;
    }

    private static void DrawDescriptionSection(ReligionDetailViewModel vm, ImDrawListPtr drawList, ref float currentY)
    {
        var font = ImGui.GetFont();
        var headerColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.239f, 0.180f, 0.125f, 1f)); // #3D2E20
        var valueColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.573f, 0.502f, 0.416f, 1f)); // #92806A

        const float columnWidth = 270f;
        const float descColumnLeft = 291f;
        // Description header (centered like Name header)
        var descCenter = vm.X + descColumnLeft + (columnWidth / 2f);
        var descHeader = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DETAIL_DESCRIPTION);
        var descHeaderWidth = ImGui.CalcTextSize(descHeader).X;
        drawList.AddText(font, 16f, new Vector2(descCenter - descHeaderWidth / 2f, currentY), headerColor,
            descHeader);

        currentY += 28f;

        // Description text (left-aligned from column 1, with reasonable max width)
        var maxWidth = 544; // Span 2 columns worth of width
        var lineHeight = 20f;

        // Simple wrapping: use ImGui text wrapping by drawing text blocks
        ImGui.PushFont(ImGui.GetFont());
        var descLines = WrapText(vm.Description, maxWidth, font, 13f);

        foreach (var line in descLines)
        {
            drawList.AddText(font, 13f, new Vector2(descCenter, currentY), valueColor, line);
            currentY += lineHeight;
        }

        ImGui.PopFont();
        currentY += 16f; // Extra spacing after description
    }

    private static List<string> WrapText(string text, float maxWidth, ImFontPtr font, float fontSize)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            // Simple approximation: assume each character is roughly fontSize * 0.5f wide
            var estimatedWidth = testLine.Length * fontSize * 0.5f;

            if (estimatedWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }

    private static void DrawMembersSection(ReligionDetailViewModel vm, ImDrawListPtr drawList, ref float currentY,
        List<DetailEvent> events)
    {
        var headerColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.239f, 0.180f, 0.125f, 1f)); // #3D2E20
        var font = ImGui.GetFont();

        // Members header
        var headerText = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DETAIL_MEMBERS, vm.MemberCount);
        drawList.AddText(font, 16f, new Vector2(vm.X + 16, currentY), headerColor, headerText);

        currentY += 28f;

        // Member list (scrollable)
        var listHeight = vm.Height - (currentY - vm.Y) - 16f;
        var members = vm.Members?.ToList() ?? new List<ReligionDetailResponsePacket.MemberInfo>();

        var newScrollY = ScrollableList.Draw<ReligionDetailResponsePacket.MemberInfo>(
            drawList,
            vm.X,
            currentY,
            vm.Width,
            listHeight,
            members,
            36f, // Item height
            8f, // Item spacing
            vm.MemberScrollY,
            (member, cx, cy, cw, ch) => DrawMemberRow(member, cx, cy, cw, ch, drawList),
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DETAIL_NO_MEMBERS)
        );

        // Emit scroll event if changed
        if (Math.Abs(newScrollY - vm.MemberScrollY) > 0.001f)
            events.Add(new DetailEvent.MemberScrollChanged(newScrollY));

        currentY += listHeight;
    }

    private static void DrawMemberRow(ReligionDetailResponsePacket.MemberInfo member, float x, float y, float width,
        float height, ImDrawListPtr drawList)
    {
        var bgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.161f, 0.118f, 0.086f, 1f)); // #291E16
        var borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.239f, 0.180f, 0.125f, 1f)); // #3D2E20
        var textColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.573f, 0.502f, 0.416f, 1f)); // #92806A

        // Background
        drawList.AddRectFilled(
            new Vector2(x, y),
            new Vector2(x + width, y + height),
            bgColor);

        // Border
        drawList.AddRect(
            new Vector2(x, y),
            new Vector2(x + width, y + height),
            borderColor, 0f, ImDrawFlags.None, 2f);

        var font = ImGui.GetFont();

        // Member name (left-aligned)
        drawList.AddText(font, 13f,
            new Vector2(x + 16f, y + (height - 16f) / 2f),
            textColor, member.PlayerName);

        // Favor rank (right-aligned - approximate positioning)
        var rankApproxWidth = member.FavorRank.Length * 7f; // Rough approximation
        drawList.AddText(font, 13f,
            new Vector2(x + width - 16f - rankApproxWidth, y + (height - 16f) / 2f),
            textColor, member.FavorRank);
    }

    /// <summary>
    ///     Get display name for a deity with full title (multi-line format)
    /// </summary>
    private static string GetDomainDisplayTitle(DeityDomain deity)
    {
        var name = deity.ToLocalizedString();
        var title = deity switch
        {
            DeityDomain.Craft => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_CRAFT_TITLE),
            DeityDomain.Wild => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_WILD_TITLE),
            DeityDomain.Harvest => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_HARVEST_TITLE),
            DeityDomain.Stone => LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_STONE_TITLE),
            _ => ""
        };

        return string.IsNullOrEmpty(title) ? name : $"{name}\n{title}";
    }

    /// <summary>
    ///     Get display name for a custom-named deity with domain title (multi-line format)
    ///     Format: "CustomName\nDomain of X" (e.g., "Ironforge\nDomain of the Forge & Craft")
    /// </summary>
    private static string GetDeityDisplayNameWithCustomName(string customName, DeityDomain deity)
    {
        var title = DomainHelper.GetDeityTitle(deity);
        return $"{customName}\n{title}";
    }

    /// <summary>
    ///     Convert prestige rank name to rank number
    /// </summary>
    private static int GetPrestigeRankNumber(string rankName)
    {
        return rankName switch
        {
            "Fledgling" => 0,
            "Established" => 1,
            "Renowned" => 2,
            "Legendary" => 3,
            "Mythic" => 4,
            _ => 0
        };
    }

    /// <summary>
    ///     Get localized prestige rank name from string
    /// </summary>
    private static string GetLocalizedPrestigeRankName(string rankName)
    {
        return rankName switch
        {
            "Fledgling" => LocalizationService.Instance.Get(LocalizationKeys.RANK_PRESTIGE_FLEDGLING),
            "Established" => LocalizationService.Instance.Get(LocalizationKeys.RANK_PRESTIGE_ESTABLISHED),
            "Renowned" => LocalizationService.Instance.Get(LocalizationKeys.RANK_PRESTIGE_RENOWNED),
            "Legendary" => LocalizationService.Instance.Get(LocalizationKeys.RANK_PRESTIGE_LEGENDARY),
            "Mythic" => LocalizationService.Instance.Get(LocalizationKeys.RANK_PRESTIGE_MYTHIC),
            _ => rankName
        };
    }
}