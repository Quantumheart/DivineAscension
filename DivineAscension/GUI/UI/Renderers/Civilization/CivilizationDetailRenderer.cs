using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Detail;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Civilization;
using DivineAscension.Services;
using DivineAscension.Systems;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Renders a detailed view of any civilization's public information.
///     Used when clicking "View Details" from the Browse tab.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationDetailRenderer
{
    public static CivilizationDetailRendererResult Draw(
        CivilizationDetailViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<DetailEvent>();
        var currentY = vm.Y;

        if (vm.IsLoading)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_LOADING),
                vm.X, currentY + 8f, vm.Width);
            return new CivilizationDetailRendererResult(events, vm.Height);
        }

        // Back button
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_BACK),
                vm.X, currentY, 160f, 32f, directoryPath: "GUI", iconName: "back"))
            events.Add(new DetailEvent.BackToBrowseClicked());

        currentY += Spacing.BackButtonRow;

        // Civilization header with rank
        TextRenderer.DrawLabel(drawList, vm.CivName, vm.X, currentY, PageTitle, ColorPalette.White);
        var civNameWidth = ImGui.CalcTextSize(vm.CivName).X * (PageTitle / SubsectionLabel); // Approximate scaled width
        var rankName = RankRequirements.GetCivilizationRankName(vm.Rank);
        var rankText = $"[{rankName}]";
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, new Vector2(vm.X + civNameWidth + 12f, currentY + 3f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), rankText);
        currentY += Spacing.Block;

        // Info grid
        var leftCol = vm.X;
        var rightCol = vm.X + vm.Width / 2f;

        // Founded date
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_FOUNDED),
            leftCol, currentY, Body, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), vm.CreatedDate.ToString("yyyy-MM-dd"));

        // Member count
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_MEMBERS),
            rightCol, currentY, Body, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), $"{vm.MemberCount}/4");

        currentY += Spacing.Section;

        // Civilization founder (player name)
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_FOUNDER),
            leftCol, currentY, Body, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), vm.FounderName);

        currentY += Spacing.Section;

        // Founding religion
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_FOUNDING_RELIGION),
            leftCol, currentY, Body, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), vm.FounderReligionName);

        currentY += Spacing.Block;

        // Description section (if present)
        if (!string.IsNullOrEmpty(vm.Description))
        {
            currentY += Spacing.Tight;
            TextRenderer.DrawLabel(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_DESCRIPTION),
                vm.X, currentY, TableHeader, ColorPalette.Grey);
            currentY += Spacing.HeaderToContent;

            TextRenderer.DrawInfoText(drawList, vm.Description, vm.X, currentY, vm.Width * 0.9f);
            currentY += 60f;
        }

        // Divider line
        drawList.AddLine(new Vector2(vm.X, currentY), new Vector2(vm.X + vm.Width, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.5f), 1f);
        currentY += Spacing.Comfortable;

        // Member religions section
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_MEMBER_RELIGIONS),
            vm.X, currentY, TableHeader, ColorPalette.White);
        currentY += Spacing.HeaderToContent;

        // Member list (scrollable)
        var listHeight = vm.Height - (currentY - vm.Y) - 80f;
        var membersList = vm.MemberReligions?.ToList() ?? new List<CivilizationInfoResponsePacket.MemberReligion>();

        var newScrollY = ScrollableList.Draw(
            drawList,
            vm.X,
            currentY,
            vm.Width,
            listHeight,
            membersList,
            60f,
            Spacing.ListItemGap,
            vm.MemberScrollY,
            (member, cx, cy, cw, ch) => DrawMemberRow(member, cx, cy, cw, ch, drawList, vm.FounderReligionName),
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_NO_MEMBERS)
        );

        // Emit scroll event if changed
        if (newScrollY != vm.MemberScrollY)
            events.Add(new DetailEvent.MemberScrollChanged(newScrollY));

        currentY += listHeight + 16f;

        // Join/Request info
        if (vm.CanRequestToJoin)
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_CAN_RECEIVE_INVITE), vm.X,
                currentY, vm.Width);
        else if (vm.IsFull)
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_FULL), vm.X, currentY,
                vm.Width);
        else
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_ALREADY_MEMBER), vm.X,
                currentY,
                vm.Width);

        return new CivilizationDetailRendererResult(events, vm.Height);
    }

    private static void DrawMemberRow(
        CivilizationInfoResponsePacket.MemberReligion member,
        float x,
        float y,
        float width,
        float height,
        ImDrawListPtr drawList,
        string founderReligionName)
    {
        // Background
        drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown), 4f);

        // Deity icon
        const float deityIconSize = 20f;
        if (Enum.TryParse<DeityDomain>(member.Domain, out var deityType))
        {
            var deityTextureId = DeityIconLoader.GetDeityTextureId(deityType);
            var iconX = x + 16f - deityIconSize / 2f;
            var iconY = y + height / 2f - deityIconSize / 2f;
            drawList.AddImage(deityTextureId,
                new Vector2(iconX, iconY),
                new Vector2(iconX + deityIconSize, iconY + deityIconSize),
                Vector2.Zero, Vector2.One,
                ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
        }

        // Religion name
        TextRenderer.DrawLabel(drawList, member.ReligionName, x + 40f, y + 8f, Body);

        // Sub info - includes deity, member count, and religion founder name
        var subText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_MEMBER_CARD_INFO,
            member.Domain, member.MemberCount, member.FounderName);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(x + 40f, y + 32f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), subText);

        // Founder badge (for the civilization's founding religion)
        if (member.ReligionName == founderReligionName)
            drawList.AddText(ImGui.GetFont(), Body, new Vector2(x + width - 120f, y + (height - 16f) / 2f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), "* Founder *");
    }
}