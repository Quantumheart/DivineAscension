using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Detail;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Civilization;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Renders a detailed view of any civilization's public information.
///     Used when clicking "View Details" from the Browse tab.
/// </summary>
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
            TextRenderer.DrawInfoText(drawList, "Loading civilization details...", vm.X, currentY + 8f, vm.Width);
            return new CivilizationDetailRendererResult(events, vm.Height);
        }

        // Back button
        if (ButtonRenderer.DrawButton(drawList, "Back to Browse", vm.X, currentY, 160f, 32f, directoryPath: "GUI",
                iconName: "back"))
            events.Add(new DetailEvent.BackToBrowseClicked());

        currentY += 44f;

        // Civilization header
        TextRenderer.DrawLabel(drawList, vm.CivName, vm.X, currentY, 20f, ColorPalette.White);
        currentY += 32f;

        // Info grid
        var leftCol = vm.X;
        var rightCol = vm.X + vm.Width / 2f;

        // Founded date
        TextRenderer.DrawLabel(drawList, "Founded:", leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), vm.CreatedDate.ToString("yyyy-MM-dd"));

        // Member count
        TextRenderer.DrawLabel(drawList, "Members:", rightCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), $"{vm.MemberCount}/4");

        currentY += 24f;

        // Civilization founder (player name)
        TextRenderer.DrawLabel(drawList, "Founder:", leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), vm.FounderName);

        currentY += 24f;

        // Founding religion
        TextRenderer.DrawLabel(drawList, "Founding Religion:", leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), vm.FounderReligionName);

        currentY += 32f;

        // Divider line
        drawList.AddLine(new Vector2(vm.X, currentY), new Vector2(vm.X + vm.Width, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.5f), 1f);
        currentY += 16f;

        // Member religions section
        TextRenderer.DrawLabel(drawList, "Member Religions", vm.X, currentY, 16f, ColorPalette.White);
        currentY += 28f;

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
            8f,
            vm.MemberScrollY,
            (member, cx, cy, cw, ch) => DrawMemberRow(member, cx, cy, cw, ch, drawList, vm.FounderReligionName),
            "No member religions."
        );

        // Emit scroll event if changed
        if (newScrollY != vm.MemberScrollY)
            events.Add(new DetailEvent.MemberScrollChanged(newScrollY));

        currentY += listHeight + 16f;

        // Join/Request info
        if (vm.CanRequestToJoin)
            TextRenderer.DrawInfoText(drawList,
                "You can receive an invitation from this civilization's founder to join.", vm.X, currentY, vm.Width);
        else if (vm.IsFull)
            TextRenderer.DrawInfoText(drawList, "This civilization is full (4/4 members).", vm.X, currentY, vm.Width);
        else
            TextRenderer.DrawInfoText(drawList, "You are already a member of a civilization.", vm.X, currentY,
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
        if (Enum.TryParse<DeityType>(member.Deity, out var deityType))
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
        TextRenderer.DrawLabel(drawList, member.ReligionName, x + 40f, y + 8f, 15f);

        // Sub info - includes deity, member count, and religion founder name
        var subText = $"Deity: {member.Deity}  |  Members: {member.MemberCount}  |  Founded by: {member.FounderName}";
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(x + 40f, y + 32f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), subText);

        // Founder badge (for the civilization's founding religion)
        if (member.ReligionName == founderReligionName)
            drawList.AddText(ImGui.GetFont(), 13f, new Vector2(x + width - 120f, y + (height - 16f) / 2f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), "* Founder *");
    }
}