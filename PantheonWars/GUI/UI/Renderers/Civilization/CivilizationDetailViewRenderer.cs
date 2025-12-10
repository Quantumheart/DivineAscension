using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network.Civilization;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

/// <summary>
///     Renders a detailed view of any civilization's public information.
///     Used when clicking "View Details" from the Browse tab.
/// </summary>
internal static class CivilizationDetailViewRenderer
{
    public static float Draw(
        GuiDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivTabState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        if (state.DetailState.IsLoading)
        {
            TextRenderer.DrawInfoText(drawList, "Loading civilization details...", x, currentY + 8f, width);
            return height;
        }

        var details = state.DetailState.ViewingCivilizationDetails;
        if (details == null)
        {
            TextRenderer.DrawInfoText(drawList, "Loading civilization details...", x, currentY + 8f, width);
            return height;
        }

        // Back button
        if (ButtonRenderer.DrawButton(drawList, "<< Back to Browse", x, currentY, 160f, 32f))
        {
            state.DetailState.ViewingCivilizationId = null;
            state.DetailState.ViewingCivilizationDetails = null;
        }

        currentY += 44f;

        // Civilization header
        TextRenderer.DrawLabel(drawList, details.Name, x, currentY, 20f, ColorPalette.White);
        currentY += 32f;

        // Info grid
        var leftCol = x;
        var rightCol = x + width / 2f;

        // Founded date
        TextRenderer.DrawLabel(drawList, "Founded:", leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), details.CreatedDate.ToString("yyyy-MM-dd"));

        // Member count
        TextRenderer.DrawLabel(drawList, "Members:", rightCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), $"{details.MemberReligions?.Count}/4");

        currentY += 24f;

        // Civilization founder (player name)
        TextRenderer.DrawLabel(drawList, "Founder:", leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), details.FounderName);

        currentY += 24f;

        // Founding religion
        TextRenderer.DrawLabel(drawList, "Founding Religion:", leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 120f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), details.FounderReligionName);

        currentY += 32f;

        // Divider line
        drawList.AddLine(new Vector2(x, currentY), new Vector2(x + width, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.5f), 1f);
        currentY += 16f;

        // Member religions section
        TextRenderer.DrawLabel(drawList, "Member Religions", x, currentY, 16f, ColorPalette.White);
        currentY += 28f;

        // Member list (scrollable)
        var listHeight = height - (currentY - y) - 80f;
        ScrollableList.Draw(
            drawList,
            x,
            currentY,
            width,
            listHeight,
            details.MemberReligions!,
            60f,
            8f,
            0f, // No scroll state needed for detail view
            (member, cx, cy, cw, ch) => DrawMemberRow(member, cx, cy, cw, ch, details.FounderReligionUID),
            "No member religions."
        );

        currentY += listHeight + 16f;

        // Join/Request info
        var isPlayerInCiv = state.InfoState.MyCivilization?.CivId == details.CivId;
        if (isPlayerInCiv)
            TextRenderer.DrawInfoText(drawList, "You are a member of this civilization.", x, currentY, width);
        else if (details.MemberReligions!.Count >= 4)
            TextRenderer.DrawInfoText(drawList, "This civilization is full (4/4 members).", x, currentY, width);
        else
            TextRenderer.DrawInfoText(drawList,
                "You can receive an invitation from this civilization's founder to join.", x, currentY, width);

        return height;
    }

    private static void DrawMemberRow(
        CivilizationInfoResponsePacket.MemberReligion member,
        float x,
        float y,
        float width,
        float height,
        string founderReligionUID)
    {
        var drawList = ImGui.GetWindowDrawList();

        // Background
        drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown), 4f);

        // Deity indicator
        var deityColor = DeityHelper.GetDeityColor(member.Deity);
        drawList.AddCircleFilled(new Vector2(x + 16f, y + height / 2f), 10f, ImGui.ColorConvertFloat4ToU32(deityColor));

        // Religion name
        TextRenderer.DrawLabel(drawList, member.ReligionName, x + 40f, y + 8f, 15f);

        // Sub info - includes deity, member count, and religion founder name
        var subText = $"Deity: {member.Deity}  |  Members: {member.MemberCount}  |  Founded by: {member.FounderName}";
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(x + 40f, y + 32f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), subText);

        // Founder badge (for the civilization's founding religion)
        if (member.ReligionId == founderReligionUID)
            drawList.AddText(ImGui.GetFont(), 13f, new Vector2(x + width - 120f, y + (height - 16f) / 2f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), "* Founder *");
    }
}