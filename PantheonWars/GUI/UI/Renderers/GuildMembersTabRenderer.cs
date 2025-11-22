using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Renderers.Components;
using PantheonWars.GUI.UI.State;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers;

/// <summary>
///     Members tab renderer for viewing and managing guild members
///     Displays member list with kick/ban actions (for leaders)
/// </summary>
[ExcludeFromCodeCoverage]
internal static class GuildMembersTabRenderer
{
    /// <summary>
    ///     Draw the guild members tab
    /// </summary>
    /// <param name="api">Client API</param>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Available width</param>
    /// <param name="height">Available height</param>
    /// <param name="state">Religion management state with member info</param>
    /// <param name="onKickMember">Callback when kick button clicked (memberUID)</param>
    /// <param name="onBanMember">Callback when ban button clicked (memberUID)</param>
    /// <returns>Updated scroll position</returns>
    public static float Draw(
        ICoreClientAPI api,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        ReligionManagementState state,
        Action<string> onKickMember,
        Action<string> onBanMember)
    {
        const float padding = 20f;
        const float sectionSpacing = 16f;

        var currentY = y + padding;

        if (state.ReligionInfo == null)
        {
            // Loading state
            var loadingText = "Loading member information...";
            var loadingSize = ImGui.CalcTextSize(loadingText);
            var loadingPos = new Vector2(x + (width - loadingSize.X) / 2, y + height / 2 - loadingSize.Y / 2);
            var loadingColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), 16f, loadingPos, loadingColor, loadingText);
            return 0f;
        }

        // === HEADER ===
        var headerText = $"Guild Members ({state.ReligionInfo.Members.Count})";
        var headerSize = ImGui.CalcTextSize(headerText);
        var headerPos = new Vector2(x + padding, currentY);
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 22f, headerPos, headerColor, headerText);

        currentY += headerSize.Y + sectionSpacing;

        // === INFO TEXT ===
        if (state.ReligionInfo.IsFounder)
        {
            var infoText = "As the guild leader, you can kick members or ban troublemakers.";
            var infoPos = new Vector2(x + padding, currentY);
            var infoColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), 14f, infoPos, infoColor, infoText);

            currentY += ImGui.CalcTextSize(infoText).Y + sectionSpacing;
        }

        // === MEMBER LIST ===
        var memberListHeight = height - (currentY - y) - padding;
        var members = state.ReligionInfo?.Members ?? new List<PlayerReligionInfoResponsePacket.MemberInfo>();

        // Only allow kick/ban if user is founder
        var kickCallback = state.ReligionInfo.IsFounder ? onKickMember : null;
        var banCallback = state.ReligionInfo.IsFounder ? onBanMember : null;

        state.MemberScrollY = MemberListRenderer.Draw(
            drawList,
            api,
            x + padding,
            currentY,
            width - padding * 2,
            memberListHeight,
            members,
            state.MemberScrollY,
            kickCallback ?? (_ => { }),
            banCallback);

        return state.MemberScrollY;
    }
}
