using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Inputs;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

internal static class CivilizationCreateRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.CivState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        TextRenderer.DrawLabel(drawList, "Create a New Civilization", x, currentY, 18f, ColorPalette.White);
        currentY += 32f;

        // Requirements
        TextRenderer.DrawLabel(drawList, "Requirements:", x, currentY, 14f, ColorPalette.Grey);
        currentY += 22f;

        var requirements = new[]
        {
            "You must be a religion founder",
            "Your religion must not be in another civilization",
            "Name must be 3-32 characters",
            "No cooldowns active"
        };

        foreach (var req in requirements)
        {
            // bullet
            drawList.AddCircleFilled(new Vector2(x + 8f, currentY + 7f), 2f, ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold));
            drawList.AddText(ImGui.GetFont(), 12f, new Vector2(x + 16f, currentY), ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), req);
            currentY += 18f;
        }

        currentY += 16f;

        // Civilization name input
        TextRenderer.DrawLabel(drawList, "Civilization Name:", x, currentY, 14f);
        currentY += 20f;

        state.CreateCivName = TextInput.Draw(drawList, "##createCivName", state.CreateCivName, x, currentY, width * 0.7f, 30f,
            placeholder: "Enter name (3-32 characters)...", maxLength: 32);
        currentY += 40f;

        // Create button
        if (ButtonRenderer.DrawButton(drawList, "Create Civilization", x, currentY, 200f, 36f, true))
        {
            if (!string.IsNullOrWhiteSpace(state.CreateCivName) && state.CreateCivName.Length >= 3)
            {
                manager.RequestCivilizationAction("create", "", "", state.CreateCivName);
                state.CreateCivName = string.Empty;
            }
            else
            {
                api.ShowChatMessage("Civilization name must be 3-32 characters.");
            }
        }

        // Clear button
        if (ButtonRenderer.DrawButton(drawList, "Clear", x + 210f, currentY, 80f, 36f))
        {
            state.CreateCivName = string.Empty;
        }

        currentY += 50f;

        // Info text
        TextRenderer.DrawInfoText(drawList, "Once created, you can invite 2-4 religions with different deities to join your civilization. Work together to build a powerful alliance!", x, currentY, width);
        currentY += 40f;

        return currentY - y;
    }
}
