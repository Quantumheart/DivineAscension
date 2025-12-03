using ImGuiNET;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.GUI.UI.Components.Buttons;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

internal static class CivilizationTabRenderer
{
    public static void Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivState;
        var drawList = ImGui.GetWindowDrawList();

        // Sub-tab header
        var tabX = x;
        var tabY = y;
        var tabW = width;
        var tabH = 36f;

        // Simple tab buttons: Browse | My Civilization | Invites | Create
        var tabWidth = 150f;
        var spacing = 6f;

        DrawTabButton("Browse", 0);
        DrawTabButton("My Civilization", 1);
        DrawTabButton("Invites", 2);
        DrawTabButton("Create", 3);

        void DrawTabButton(string label, int index)
        {
            var tx = tabX + index * (tabWidth + spacing);
            var isActive = state.CurrentSubTab == index;
            var clicked = ButtonRenderer.DrawButton(drawList, label, tx, tabY, tabWidth, tabH,
                isPrimary: isActive, enabled: true,
                customColor: isActive ? ColorPalette.Gold * 0.7f : ColorPalette.DarkBrown * 0.6f);
            if (clicked) state.CurrentSubTab = index;
        }

        var contentY = y + tabH + 10f;
        var contentHeight = height - (contentY - y);

        switch (state.CurrentSubTab)
        {
            case 0:
                CivilizationBrowseRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case 1:
                CivilizationManageRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case 2:
                CivilizationInvitesRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
            case 3:
                CivilizationCreateRenderer.Draw(manager, api, x, contentY, width, contentHeight);
                break;
        }
    }
}
